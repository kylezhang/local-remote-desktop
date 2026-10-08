package main

import (
	"context"
	"crypto/hmac"
	"encoding/binary"
	"encoding/json"
	"errors"
	"github.com/pion/webrtc/v4"
	"github.com/pion/webrtc/v4/pkg/media"
	"github.com/pion/webrtc/v4/pkg/media/h264reader"
	"io"
	"net"
	"os"
	"sync"
	"time"
)

// RML1 connects only to the Host on the same Mac, never sends TCP video across LAN.
type upstream struct {
	c              net.Conn
	tx, rx         []byte
	sent, received uint64
	mu             sync.Mutex
}

func nonce(n uint64) []byte { b := make([]byte, 12); binary.BigEndian.PutUint64(b[4:], n); return b }
func connectUpstream(ctx context.Context, key []byte) (*upstream, error) {
	c, e := (&net.Dialer{Timeout: 5 * time.Second}).DialContext(ctx, "tcp4", "127.0.0.1:47980")
	if e != nil {
		return nil, e
	}
	fail := func(err error) (*upstream, error) { c.Close(); return nil, err }
	c.SetDeadline(time.Now().Add(10 * time.Second))
	hello := make([]byte, 36)
	if _, e = io.ReadFull(c, hello); e != nil {
		return fail(e)
	}
	if string(hello[:4]) != "RML1" {
		return fail(errors.New("wrong Host protocol"))
	}
	client := random(32)
	transcript := append(append([]byte{}, hello[4:]...), client...)
	response := append(client, mac(key, append([]byte("client"), transcript...))...)
	if e = writeFull(c, response); e != nil {
		return fail(e)
	}
	proof := make([]byte, 32)
	if _, e = io.ReadFull(c, proof); e != nil {
		return fail(e)
	}
	if !hmac.Equal(proof, mac(key, append([]byte("server"), transcript...))) {
		return fail(errors.New("Host authentication failed"))
	}
	c.SetDeadline(time.Time{})
	return &upstream{c: c, tx: mac(key, append([]byte("c2h"), transcript...)), rx: mac(key, append([]byte("h2c"), transcript...))}, nil
}
func writeFull(w io.Writer, b []byte) error {
	for len(b) > 0 {
		n, e := w.Write(b)
		if e != nil {
			return e
		}
		if n == 0 {
			return io.ErrShortWrite
		}
		b = b[n:]
	}
	return nil
}
func (u *upstream) send(v any) error {
	u.mu.Lock()
	defer u.mu.Unlock()
	payload, e := json.Marshal(v)
	if e != nil {
		return e
	}
	plain := append([]byte{1}, payload...)
	h := make([]byte, 4)
	binary.BigEndian.PutUint32(h, uint32(len(plain)+16))
	a, e := gcm(u.tx)
	if e != nil {
		return e
	}
	body := a.Seal(nil, nonce(u.sent), plain, h)
	u.sent++
	u.c.SetWriteDeadline(time.Now().Add(3 * time.Second))
	return writeFull(u.c, append(h, body...))
}
func (u *upstream) read() (byte, []byte, error) {
	u.c.SetReadDeadline(time.Now().Add(15 * time.Second))
	h := make([]byte, 4)
	if _, e := io.ReadFull(u.c, h); e != nil {
		return 0, nil, e
	}
	n := binary.BigEndian.Uint32(h)
	if n < 17 || n > 8*1024*1024 {
		return 0, nil, errors.New("invalid Host frame size")
	}
	body := make([]byte, n)
	if _, e := io.ReadFull(u.c, body); e != nil {
		return 0, nil, e
	}
	a, e := gcm(u.rx)
	if e != nil {
		return 0, nil, e
	}
	p, e := a.Open(nil, nonce(u.received), body, h)
	if e != nil {
		return 0, nil, e
	}
	u.received++
	return p[0], p[1:], nil
}
func streamDesktop(ctx context.Context, key []byte, quality int, track *webrtc.TrackLocalStaticSample) error {
	u, e := connectUpstream(ctx, key)
	if e != nil {
		return e
	}
	defer u.c.Close()
	done := make(chan struct{})
	defer close(done)
	go func() {
		select {
		case <-ctx.Done():
			u.c.Close()
		case <-done:
		}
	}()
	if e = u.send(map[string]any{"op": "handshake", "version": 1, "quality": quality}); e != nil {
		return e
	}
	go func() {
		t := time.NewTicker(3 * time.Second)
		defer t.Stop()
		for {
			select {
			case <-done:
				return
			case <-ctx.Done():
				return
			case <-t.C:
				if u.send(map[string]any{"op": "heartbeat"}) != nil {
					u.c.Close()
					return
				}
			}
		}
	}()
	fps := 30
	last := time.Time{}
	for {
		typ, p, e := u.read()
		if e != nil {
			return e
		}
		if typ == 1 {
			var m struct {
				Op  string `json:"op"`
				FPS int    `json:"fps"`
			}
			if json.Unmarshal(p, &m) == nil && m.Op == "video_header" && m.FPS > 0 {
				fps = m.FPS
			}
		} else if typ == 2 {
			duration := time.Second / time.Duration(fps)
			now := time.Now()
			if !last.IsZero() {
				duration = now.Sub(last)
			}
			last = now
			if e = track.WriteSample(media.Sample{Data: p, Duration: duration}); e != nil {
				return e
			}
		}
	}
}

// Synthetic input must contain AUD NALs. Only used in the independent test mode.
func readAccessUnits(path string) ([][]byte, error) {
	f, e := os.Open(path)
	if e != nil {
		return nil, e
	}
	defer f.Close()
	r, e := h264reader.NewReader(f)
	if e != nil {
		return nil, e
	}
	var units [][]byte
	var current []byte
	for {
		nal, e := r.NextNAL()
		if e == io.EOF {
			break
		}
		if e != nil {
			return nil, e
		}
		if nal.UnitType == h264reader.NalUnitTypeAUD && len(current) > 0 {
			units = append(units, current)
			current = nil
		}
		current = append(current, 0, 0, 0, 1)
		current = append(current, nal.Data...)
	}
	if len(current) > 0 {
		units = append(units, current)
	}
	if len(units) == 0 {
		return nil, errors.New("no H264 samples")
	}
	return units, nil
}
func streamSynthetic(ctx context.Context, units [][]byte, fps int, track *webrtc.TrackLocalStaticSample) error {
	t := time.NewTicker(time.Second / time.Duration(fps))
	defer t.Stop()
	i := 0
	for {
		select {
		case <-ctx.Done():
			return ctx.Err()
		case <-t.C:
			if e := track.WriteSample(media.Sample{Data: units[i], Duration: time.Second / time.Duration(fps)}); e != nil {
				return e
			}
			i = (i + 1) % len(units)
		}
	}
}
