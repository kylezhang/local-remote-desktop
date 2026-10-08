package main

import (
	"bytes"
	"context"
	"encoding/json"
	"github.com/pion/webrtc/v4"
	"net"
	"net/http"
	"net/http/httptest"
	"os"
	"testing"
	"time"
)

func TestSignalAuthentication(t *testing.T) {
	key := random(32)
	challenge := "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
	packet, e := sealSignal(key, "rtc-offer-v1", challenge, []byte("SDP with authenticated DTLS fingerprint"))
	if e != nil {
		t.Fatal(e)
	}
	plain, e := openSignal(key, "rtc-offer-v1", packet)
	if e != nil || string(plain) != "SDP with authenticated DTLS fingerprint" {
		t.Fatal("roundtrip failed")
	}
	if _, e = openSignal(key, "rtc-answer-v1", packet); e == nil {
		t.Fatal("reflected request accepted as answer")
	}
	if _, e = openSignal(random(32), "rtc-offer-v1", packet); e == nil {
		t.Fatal("wrong key accepted")
	}
	packet.Challenge = "1123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
	if _, e = openSignal(key, "rtc-offer-v1", packet); e == nil {
		t.Fatal("changed challenge accepted")
	}
}
func TestNetworkGuards(t *testing.T) {
	for _, s := range []string{"127.0.0.1", "10.1.2.3", "172.16.0.1", "192.168.1.2", "169.254.1.2"} {
		if !localIP(net.ParseIP(s)) {
			t.Fatal(s)
		}
	}
	for _, s := range []string{"8.8.8.8", "172.32.0.1", "::1"} {
		if localIP(net.ParseIP(s)) {
			t.Fatal(s)
		}
	}
	handler := viewerGuard(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) { w.WriteHeader(204) }))
	for _, origin := range []string{"http://evil.example", "http://127.0.0.1:47982"} {
		r := httptest.NewRequest("POST", "http://127.0.0.1:47982/signal", nil)
		r.Header.Set("Content-Type", "application/json")
		r.Header.Set("Origin", origin)
		w := httptest.NewRecorder()
		handler.ServeHTTP(w, r)
		expected := 403
		if origin == "http://127.0.0.1:47982" {
			expected = 204
		}
		if w.Code != expected {
			t.Fatal(w.Code)
		}
	}
}
func TestChallengeExpiryAndReplay(t *testing.T) {
	key := random(32)
	h := &hostServer{key: key, challenges: make(map[string]time.Time)}
	for _, state := range []string{"expired", "absent"} {
		c := "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
		if state == "expired" {
			h.challenges[c] = time.Now().Add(-time.Second)
		}
		p, _ := sealSignal(key, "rtc-offer-v1", c, []byte(`{"type":"offer","sdp":"invalid"}`))
		raw, _ := json.Marshal(p)
		r := httptest.NewRequest("POST", "/offer", bytes.NewReader(raw))
		w := httptest.NewRecorder()
		h.offer(w, r)
		if w.Code != 403 {
			t.Fatalf("%s accepted: %d", state, w.Code)
		}
	}
}
func TestWebRTCVideoOverUDP(t *testing.T) {
	path := os.Getenv("RML_RTC_SAMPLE_PATH")
	if path == "" {
		t.Skip("set RML_RTC_SAMPLE_PATH to AUD H264 fixture for media integration test")
	}
	units, e := readAccessUnits(path)
	if e != nil {
		t.Fatal(e)
	}
	key := random(32)
	h := &hostServer{key: key, units: units, fps: 30, challenges: make(map[string]time.Time)}
	settings := webrtc.SettingEngine{}
	settings.SetNetworkTypes([]webrtc.NetworkType{webrtc.NetworkTypeUDP4})
	settings.SetIncludeLoopbackCandidate(true)
	pc, e := webrtc.NewAPI(webrtc.WithSettingEngine(settings)).NewPeerConnection(webrtc.Configuration{})
	if e != nil {
		t.Fatal(e)
	}
	defer pc.Close()
	ctx, cancel := context.WithTimeout(context.Background(), 15*time.Second)
	defer cancel()
	received := make(chan struct{}, 1)
	pc.OnTrack(func(track *webrtc.TrackRemote, _ *webrtc.RTPReceiver) {
		if track.Codec().MimeType != webrtc.MimeTypeH264 {
			t.Errorf("codec %s", track.Codec().MimeType)
		}
		for i := 0; i < 20; i++ {
			p, _, e := track.ReadRTP()
			if e != nil {
				return
			}
			if len(p.Payload) == 0 {
				t.Error("empty H264 payload")
				return
			}
		}
		received <- struct{}{}
	})
	if _, e = pc.AddTransceiverFromKind(webrtc.RTPCodecTypeVideo, webrtc.RTPTransceiverInit{Direction: webrtc.RTPTransceiverDirectionRecvonly}); e != nil {
		t.Fatal(e)
	}
	offer, e := pc.CreateOffer(nil)
	if e != nil {
		t.Fatal(e)
	}
	gathered := webrtc.GatheringCompletePromise(pc)
	if e = pc.SetLocalDescription(offer); e != nil {
		t.Fatal(e)
	}
	select {
	case <-gathered:
	case <-ctx.Done():
		t.Fatal("offer timeout")
	}
	cw := httptest.NewRecorder()
	h.challenge(cw, httptest.NewRequest("GET", "/challenge", nil))
	var c struct {
		Challenge string `json:"challenge"`
	}
	json.Unmarshal(cw.Body.Bytes(), &c)
	raw, _ := json.Marshal(pc.LocalDescription())
	p, e := sealSignal(key, "rtc-offer-v1", c.Challenge, raw)
	if e != nil {
		t.Fatal(e)
	}
	raw, _ = json.Marshal(p)
	aw := httptest.NewRecorder()
	h.offer(aw, httptest.NewRequest("POST", "/offer", bytes.NewReader(raw)))
	if aw.Code != 200 {
		t.Fatalf("offer %d: %s", aw.Code, aw.Body.String())
	}
	var reply envelope
	json.Unmarshal(aw.Body.Bytes(), &reply)
	plain, e := openSignal(key, "rtc-answer-v1", reply)
	if e != nil {
		t.Fatal(e)
	}
	var answer webrtc.SessionDescription
	if e = json.Unmarshal(plain, &answer); e != nil {
		t.Fatal(e)
	}
	if e = pc.SetRemoteDescription(answer); e != nil {
		t.Fatal(e)
	}
	defer func() {
		h.mu.Lock()
		serverPeer := h.peer
		h.mu.Unlock()
		if serverPeer != nil {
			serverPeer.Close()
		}
	}()
	select {
	case <-received:
		t.Log("H264 RTP packets received through ICE/DTLS/SRTP over UDP")
	case <-ctx.Done():
		t.Fatal("no video RTP within 15s")
	}
	// The same authenticated offer envelope cannot create another session.
	rw := httptest.NewRecorder()
	h.offer(rw, httptest.NewRequest("POST", "/offer", bytes.NewReader(raw)))
	if rw.Code != 403 {
		t.Fatalf("replayed offer accepted: %d", rw.Code)
	}
}
