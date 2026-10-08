package main

import (
	"bufio"
	"context"
	"embed"
	"encoding/hex"
	"encoding/json"
	"errors"
	"flag"
	"fmt"
	"github.com/pion/ice/v4"
	"github.com/pion/webrtc/v4"
	"golang.org/x/term"
	"io"
	"io/fs"
	"log"
	"net"
	"net/http"
	"os"
	"strings"
	"sync"
	"time"
)

//go:embed web/*
var assets embed.FS

func localIP(ip net.IP) bool {
	return ip != nil && ip.To4() != nil && (ip.IsPrivate() || ip.IsLoopback() || ip.IsLinkLocalUnicast())
}
func lanOnly(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		host, _, e := net.SplitHostPort(r.RemoteAddr)
		if e != nil || !localIP(net.ParseIP(host)) {
			http.Error(w, "LAN only", 403)
			return
		}
		w.Header().Set("Cache-Control", "no-store")
		next.ServeHTTP(w, r)
	})
}

type hostServer struct {
	key        []byte
	quality    int
	units      [][]byte
	fps        int
	mu         sync.Mutex
	challenges map[string]time.Time
	peer       *webrtc.PeerConnection
}

func (h *hostServer) challenge(w http.ResponseWriter, r *http.Request) {
	if r.Method != "GET" {
		http.Error(w, "GET required", 405)
		return
	}
	h.mu.Lock()
	defer h.mu.Unlock()
	now := time.Now()
	for c, expiry := range h.challenges {
		if now.After(expiry) {
			delete(h.challenges, c)
		}
	}
	if len(h.challenges) >= 128 {
		http.Error(w, "busy", 429)
		return
	}
	c := hex.EncodeToString(random(32))
	h.challenges[c] = now.Add(30 * time.Second)
	writeJSON(w, map[string]string{"challenge": c})
}
func writeJSON(w http.ResponseWriter, v any) {
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(v)
}
func decode(w http.ResponseWriter, r *http.Request, v any) error {
	return json.NewDecoder(http.MaxBytesReader(w, r.Body, 256*1024)).Decode(v)
}
func (h *hostServer) offer(w http.ResponseWriter, r *http.Request) {
	if r.Method != "POST" {
		http.Error(w, "POST required", 405)
		return
	}
	var p envelope
	if e := decode(w, r, &p); e != nil {
		http.Error(w, "invalid request", 400)
		return
	}
	plain, e := openSignal(h.key, "rtc-offer-v1", p)
	if e != nil {
		http.Error(w, "authentication failed", 403)
		return
	}
	h.mu.Lock()
	expiry, ok := h.challenges[p.Challenge]
	delete(h.challenges, p.Challenge)
	if !ok || time.Now().After(expiry) {
		h.mu.Unlock()
		http.Error(w, "expired or reused challenge", 403)
		return
	}
	if h.peer != nil {
		h.mu.Unlock()
		http.Error(w, "one viewer already connected", 409)
		return
	}
	var offer webrtc.SessionDescription
	if json.Unmarshal(plain, &offer) != nil || offer.Type != webrtc.SDPTypeOffer {
		h.mu.Unlock()
		http.Error(w, "invalid offer", 400)
		return
	}
	settings := webrtc.SettingEngine{}
	settings.SetNetworkTypes([]webrtc.NetworkType{webrtc.NetworkTypeUDP4})
	settings.SetIncludeLoopbackCandidate(true)
	settings.SetIPFilter(localIP)
	settings.SetRemoteIPFilter(localIP)
	settings.SetICEMulticastDNSMode(ice.MulticastDNSModeDisabled)
	if e = settings.SetEphemeralUDPPortRange(47990, 48010); e != nil {
		h.mu.Unlock()
		http.Error(w, "UDP setup failed", 500)
		return
	}
	pc, e := webrtc.NewAPI(webrtc.WithSettingEngine(settings)).NewPeerConnection(webrtc.Configuration{ICEServers: []webrtc.ICEServer{}})
	if e != nil {
		h.mu.Unlock()
		http.Error(w, "peer setup failed", 500)
		return
	}
	h.peer = pc
	h.mu.Unlock()
	ctx, cancel := context.WithCancel(context.Background())
	var once sync.Once
	cleanup := func() {
		once.Do(func() {
			cancel()
			go pc.Close()
			h.mu.Lock()
			if h.peer == pc {
				h.peer = nil
			}
			h.mu.Unlock()
		})
	}
	success := false
	defer func() {
		if !success {
			cleanup()
		}
	}()
	track, e := webrtc.NewTrackLocalStaticSample(webrtc.RTPCodecCapability{MimeType: webrtc.MimeTypeH264, ClockRate: 90000, SDPFmtpLine: "level-asymmetry-allowed=1;packetization-mode=1;profile-level-id=42e01f"}, "desktop", "remote-mac-lan")
	if e != nil {
		http.Error(w, "track failed", 500)
		return
	}
	sender, e := pc.AddTrack(track)
	if e != nil {
		http.Error(w, "track failed", 500)
		return
	}
	go func() {
		buffer := make([]byte, 1500)
		for {
			if _, _, err := sender.Read(buffer); err != nil {
				return
			}
		}
	}()
	var started sync.Once
	pc.OnConnectionStateChange(func(state webrtc.PeerConnectionState) {
		log.Printf("WebRTC state: %s", state.String())
		if state == webrtc.PeerConnectionStateConnected {
			started.Do(func() {
				go func() {
					var err error
					if h.units != nil {
						err = streamSynthetic(ctx, h.units, h.fps, track)
					} else {
						err = streamDesktop(ctx, h.key, h.quality, track)
					}
					if err != nil && ctx.Err() == nil {
						log.Printf("video source stopped: %v", err)
					}
					cleanup()
				}()
			})
		}
		if state == webrtc.PeerConnectionStateFailed || state == webrtc.PeerConnectionStateClosed || state == webrtc.PeerConnectionStateDisconnected {
			cleanup()
		}
	})
	// Reserve the single viewer slot for at most 25 seconds if ICE never connects.
	time.AfterFunc(25*time.Second, func() {
		if pc.ConnectionState() != webrtc.PeerConnectionStateConnected {
			cleanup()
		}
	})
	if e = pc.SetRemoteDescription(offer); e != nil {
		http.Error(w, "offer rejected", 400)
		return
	}
	answer, e := pc.CreateAnswer(nil)
	if e != nil {
		http.Error(w, "answer failed", 500)
		return
	}
	gathered := webrtc.GatheringCompletePromise(pc)
	if e = pc.SetLocalDescription(answer); e != nil {
		http.Error(w, "description failed", 500)
		return
	}
	select {
	case <-gathered:
	case <-r.Context().Done():
		return
	case <-time.After(10 * time.Second):
		http.Error(w, "ICE gathering timed out", 504)
		return
	}
	raw, e := json.Marshal(pc.LocalDescription())
	if e != nil {
		http.Error(w, "answer failed", 500)
		return
	}
	reply, e := sealSignal(h.key, "rtc-answer-v1", p.Challenge, raw)
	if e != nil {
		http.Error(w, "answer failed", 500)
		return
	}
	success = true
	writeJSON(w, reply)
}
func readKey() ([]byte, error) {
	fmt.Fprint(os.Stderr, "Paste Mac pairing key (64 hex characters; hidden): ")
	var b []byte
	var e error
	if term.IsTerminal(int(os.Stdin.Fd())) {
		b, e = term.ReadPassword(int(os.Stdin.Fd()))
		fmt.Fprintln(os.Stderr)
	} else {
		var s string
		s, e = bufio.NewReader(os.Stdin).ReadString('\n')
		b = []byte(s)
	}
	if e != nil {
		return nil, e
	}
	key, e := hex.DecodeString(strings.TrimSpace(string(b)))
	if e != nil || len(key) != 32 {
		return nil, errors.New("invalid pairing key")
	}
	return key, nil
}
func signalProxy(w http.ResponseWriter, r *http.Request) {
	if r.Method != "POST" {
		http.Error(w, "POST required", 405)
		return
	}
	var v struct {
		Host  string                    `json:"host"`
		Key   string                    `json:"key"`
		Offer webrtc.SessionDescription `json:"offer"`
	}
	if decode(w, r, &v) != nil {
		http.Error(w, "invalid request", 400)
		return
	}
	ip := net.ParseIP(v.Host)
	key, e := hex.DecodeString(strings.TrimSpace(v.Key))
	if !localIP(ip) || e != nil || len(key) != 32 {
		http.Error(w, "LAN IPv4 and 64-character pairing key required", 400)
		return
	}
	transport := &http.Transport{Proxy: nil}
	defer transport.CloseIdleConnections()
	client := &http.Client{Transport: transport, Timeout: 15 * time.Second}
	base := "http://" + net.JoinHostPort(ip.String(), "47981")
	get, e := http.NewRequestWithContext(r.Context(), "GET", base+"/challenge", nil)
	if e != nil {
		http.Error(w, "request failed", 500)
		return
	}
	response, e := client.Do(get)
	if e != nil {
		http.Error(w, "cannot reach Mac experiment bridge", 502)
		return
	}
	var c struct {
		Challenge string `json:"challenge"`
	}
	e = json.NewDecoder(io.LimitReader(response.Body, 4096)).Decode(&c)
	response.Body.Close()
	if e != nil || response.StatusCode != 200 {
		http.Error(w, "challenge failed", 502)
		return
	}
	raw, e := json.Marshal(v.Offer)
	if e != nil {
		http.Error(w, "offer invalid", 400)
		return
	}
	p, e := sealSignal(key, "rtc-offer-v1", c.Challenge, raw)
	if e != nil {
		http.Error(w, "challenge invalid", 502)
		return
	}
	raw, _ = json.Marshal(p)
	post, e := http.NewRequestWithContext(r.Context(), "POST", base+"/offer", strings.NewReader(string(raw)))
	if e != nil {
		http.Error(w, "request failed", 500)
		return
	}
	post.Header.Set("Content-Type", "application/json")
	response, e = client.Do(post)
	if e != nil {
		http.Error(w, "Mac negotiation failed", 502)
		return
	}
	defer response.Body.Close()
	if response.StatusCode != 200 {
		http.Error(w, "Mac rejected negotiation (pairing key, occupied session, or expired challenge)", response.StatusCode)
		return
	}
	var answer envelope
	if e = json.NewDecoder(io.LimitReader(response.Body, 256*1024)).Decode(&answer); e != nil || answer.Challenge != c.Challenge {
		http.Error(w, "answer invalid", 502)
		return
	}
	plain, e := openSignal(key, "rtc-answer-v1", answer)
	if e != nil {
		http.Error(w, "Mac answer authentication failed", 403)
		return
	}
	w.Header().Set("Content-Type", "application/json")
	w.Write(plain)
}
func viewerGuard(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.Host != "127.0.0.1:47982" {
			http.Error(w, "invalid host", 403)
			return
		}
		if r.Method == "POST" && (r.Header.Get("Origin") != "http://127.0.0.1:47982" || !strings.HasPrefix(r.Header.Get("Content-Type"), "application/json")) {
			http.Error(w, "invalid origin", 403)
			return
		}
		w.Header().Set("Cache-Control", "no-store")
		w.Header().Set("X-Content-Type-Options", "nosniff")
		w.Header().Set("Content-Security-Policy", "default-src 'self'; script-src 'self'; style-src 'self'; connect-src 'self'; media-src 'self' blob:; frame-ancestors 'none'")
		next.ServeHTTP(w, r)
	})
}
func main() {
	mode := flag.String("mode", "viewer", "viewer or host")
	quality := flag.Int("quality", 0, "0=720p30, 1=1080p60, 2=1080p60 high bitrate")
	synthetic := flag.String("synthetic", "", "test-only AUD H264 file, never records the screen")
	fps := flag.Int("fps", 30, "synthetic frame rate")
	flag.Parse()
	mux := http.NewServeMux()
	var address string
	var handler http.Handler
	switch *mode {
	case "host":
		if *quality < 0 || *quality > 2 || *fps < 1 || *fps > 120 {
			log.Fatal("invalid quality/fps")
		}
		key, e := readKey()
		if e != nil {
			log.Fatal(e)
		}
		h := &hostServer{key: key, quality: *quality, fps: *fps, challenges: make(map[string]time.Time)}
		if *synthetic != "" {
			h.units, e = readAccessUnits(*synthetic)
			if e != nil {
				log.Fatal(e)
			}
		}
		mux.HandleFunc("/challenge", h.challenge)
		mux.HandleFunc("/offer", h.offer)
		address = "0.0.0.0:47981"
		handler = lanOnly(mux)
		log.Print("Experiment host: TCP 47981 signaling, UDP 47990-48010 media, LAN-only; one viewer; view-only")
	case "viewer":
		sub, e := fs.Sub(assets, "web")
		if e != nil {
			log.Fatal(e)
		}
		mux.Handle("/", http.FileServer(http.FS(sub)))
		mux.HandleFunc("/signal", signalProxy)
		address = "127.0.0.1:47982"
		handler = viewerGuard(mux)
		log.Print("Open http://127.0.0.1:47982 (view-only experiment)")
	default:
		log.Fatal("mode must be viewer or host")
	}
	server := http.Server{Addr: address, Handler: handler, ReadHeaderTimeout: 5 * time.Second, ReadTimeout: 20 * time.Second, WriteTimeout: 20 * time.Second, IdleTimeout: 30 * time.Second, MaxHeaderBytes: 8192}
	log.Fatal(server.ListenAndServe())
}
