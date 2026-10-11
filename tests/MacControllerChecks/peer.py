"""Independent loopback peer. Synthetic credentials/video; never injects OS input."""
import hashlib, hmac, json, os, socket, struct, subprocess, sys, threading
from cryptography.hazmat.primitives.ciphers.aead import AESGCM

def check(binary, mode, session=False):
    listener = socket.socket(); listener.bind(('127.0.0.1', 0)); listener.listen(1); listener.settimeout(10)
    process = subprocess.Popen([binary, mode, str(listener.getsockname()[1])])
    try:
        conn, _ = listener.accept(); conn.settimeout(8)
        with conn:
            def read(n):
                out = b''
                while len(out) < n:
                    block = conn.recv(n-len(out))
                    if not block: raise EOFError()
                    out += block
                return out
            server = os.urandom(32); salt = bytes(range(16)); legacy = mode == 'legacy'
            key = bytes.fromhex('5c'*32) if legacy else hashlib.pbkdf2_hmac('sha256', b'Office-LAN#42', salt, 600_000, 32)
            conn.sendall((b'RML1' if legacy else b'RMP1')+server+(b'' if legacy else salt))
            response = read(64); transcript = server+response[:32] if legacy else b'RMP1'+salt+server+response[:32]
            mac = lambda label: hmac.digest(key,label+transcript,'sha256')
            assert hmac.compare_digest(response[32:],mac(b'client'))
            proof = mac(b'server')
            if mode == 'auth-eof': conn.shutdown(socket.SHUT_RDWR)
            else: conn.sendall(bytes([proof[0]^1])+proof[1:] if mode == 'bad-proof' else proof)
            if mode == 'auth-eof': pass
            elif mode == 'bad-proof':
                assert conn.recv(1) == b''
            else:
                tx, rx = AESGCM(mac(b'h2c')), AESGCM(mac(b'c2h')); sent = received = 0
                def nonce(n): return b'\0'*4+struct.pack('>Q',n)
                def receive():
                    nonlocal received
                    header = read(4); size, = struct.unpack('>I',header); assert 17 <= size <= 65536
                    data = rx.decrypt(nonce(received),read(size),header); received += 1
                    assert data[0] == 1
                    return json.loads(data[1:])
                def packet(kind, payload):
                    nonlocal sent
                    header = struct.pack('>I',len(payload)+17)
                    data = header+tx.encrypt(nonce(sent),bytes([kind])+payload,header); sent += 1
                    return data
                def message(obj): return packet(1,json.dumps(obj,separators=(',',':')).encode())
                assert receive() == dict(op='handshake',version=1,quality=0)
                caps = message(dict(op='capabilities',platform='linux' if mode == 'reject' else 'windows',inputModel='win-vk-v1',inputEnabled=True))
                # Fragment the real challenge/records without sharing Swift code.
                for i in range(0,len(caps),3): conn.sendall(caps[i:i+3])
                if mode == 'reject': assert conn.recv(1) == b''
                elif mode == 'replay': conn.sendall(caps); assert conn.recv(1) == b''
                else:
                    conn.sendall(message(dict(op='video_header',codec='h264-annexb',width=1280,height=720,fps=30)))
                    if mode == 'clipboard':
                        def control():
                            while True:
                                value = receive()
                                if value['op'] != 'heartbeat': return value
                                conn.sendall(message(dict(op='heartbeat',inputEnabled=True)))
                        assert control() == dict(op='clipboard_sync', requestId=1, enabled=True)
                        conn.sendall(message(dict(op='clipboard_sync', requestId=1, enabled=True)))
                        assert control() == dict(op='clipboard_text', requestId=1, encoding='utf8-base64url', text='TWFjIOKGkiBXaW5kb3dz')
                        conn.sendall(message(dict(op='clipboard_text', requestId=1, encoding='utf8-base64url', text='V2luZG93cyDihpIgTWFj')))
                        assert control() == dict(op='disconnect')
                    elif mode == 'oversized': conn.sendall(struct.pack('>I',8*1024*1024+1)); assert conn.recv(1) == b''
                    elif mode == 'stop':
                        while conn.recv(4096): pass
                    else:
                        conn.sendall(packet(2,b'\x00\x00\x00\x01\x65'+b'\x11'*99_995 if session else b'\x11'*100_000))
                        values = []
                        while len(values) < (6 if session else 7):
                            value = receive()
                            if value['op'] == 'heartbeat': conn.sendall(message(dict(op='heartbeat',inputEnabled=True)))
                            else: values.append(value)
                        assert [(x['vk'],x['down']) for x in values[:4]] == [(162,True),(67,True),(67,False),(162,False)]
                        assert values[4] == dict(op='mouse_move',x=0.5,y=0.25)
                        assert values[5:] == ([dict(op='disconnect')] if session else [dict(op='release_all'),dict(op='disconnect')])
        assert process.wait(timeout=10) == 0
    finally:
        listener.close()
        if process.poll() is None: process.kill(); process.wait()

for mode in ['password','legacy','bad-proof','replay','oversized']: check(sys.argv[1],mode)
if len(sys.argv) > 2:
    for mode in ['session','stop','reject','auth-eof','clipboard']: check(sys.argv[2],mode,session=True)
print('PASS independent Python/Swift RMP1/RML1 interoperability; ' +
      ('native CryptoKit/CommonCrypto and Darwin sockets; GUI not tested' if sys.platform == 'darwin'
       else 'OpenSSL test adapters; native CryptoKit/GUI not tested'))
