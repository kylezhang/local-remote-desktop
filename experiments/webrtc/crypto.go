package main

import (
	"crypto/aes"
	"crypto/cipher"
	"crypto/hmac"
	"crypto/rand"
	"crypto/sha256"
	"encoding/base64"
	"encoding/hex"
	"errors"
)

type envelope struct {
	Challenge string `json:"challenge"`
	Nonce     string `json:"nonce"`
	Data      string `json:"data"`
}

func random(n int) []byte {
	b := make([]byte, n)
	if _, err := rand.Read(b); err != nil {
		panic(err)
	}
	return b
}
func mac(key, data []byte) []byte { h := hmac.New(sha256.New, key); h.Write(data); return h.Sum(nil) }
func gcm(key []byte) (cipher.AEAD, error) {
	b, e := aes.NewCipher(key)
	if e != nil {
		return nil, e
	}
	return cipher.NewGCM(b)
}
func sealSignal(key []byte, direction, challenge string, plain []byte) (envelope, error) {
	c, e := hex.DecodeString(challenge)
	if e != nil || len(c) != 32 {
		return envelope{}, errors.New("invalid challenge")
	}
	k := mac(key, append([]byte(direction), c...))
	a, e := gcm(k)
	if e != nil {
		return envelope{}, e
	}
	n := random(12)
	return envelope{challenge, base64.StdEncoding.EncodeToString(n), base64.StdEncoding.EncodeToString(a.Seal(nil, n, plain, []byte(challenge)))}, nil
}
func openSignal(key []byte, direction string, p envelope) ([]byte, error) {
	c, e := hex.DecodeString(p.Challenge)
	if e != nil || len(c) != 32 {
		return nil, errors.New("invalid challenge")
	}
	n, e := base64.StdEncoding.DecodeString(p.Nonce)
	if e != nil || len(n) != 12 {
		return nil, errors.New("invalid nonce")
	}
	data, e := base64.StdEncoding.DecodeString(p.Data)
	if e != nil || len(data) > 256*1024 {
		return nil, errors.New("invalid payload")
	}
	a, e := gcm(mac(key, append([]byte(direction), c...)))
	if e != nil {
		return nil, e
	}
	return a.Open(nil, n, data, []byte(p.Challenge))
}
