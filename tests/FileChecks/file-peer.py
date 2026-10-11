"""Independent loopback RMP1/file peer. No GUI, user clipboard or LAN devices."""
import hashlib, hmac, json, os, pathlib, socket, struct, subprocess, sys, tempfile
from cryptography.hazmat.primitives.ciphers.aead import AESGCM

def check(binary, mode):
    with tempfile.TemporaryDirectory(prefix='lrd-file-job-') as root:
        listener = socket.socket(); listener.setsockopt(socket.SOL_SOCKET,socket.SO_REUSEADDR,1)
        listener.bind(('127.0.0.1',47981)); listener.listen(1); listener.settimeout(10)
        process = subprocess.Popen([binary,mode,root])
        try:
            conn,_ = listener.accept()
            with conn:
                conn.settimeout(10)
                def read(n):
                    value=b''
                    while len(value)<n:
                        part=conn.recv(n-len(value))
                        if not part: raise EOFError('closed')
                        value+=part
                    return value
                salt=os.urandom(16); server=os.urandom(32)
                key=hashlib.pbkdf2_hmac('sha256',b'Fixture1',salt,600000)
                conn.sendall(b'RMP1'+server+salt); response=read(64)
                transcript=b'RMP1'+salt+server+response[:32]
                def mac(label): return hmac.digest(key,label+transcript,'sha256')
                assert hmac.compare_digest(response[32:],mac(b'client'))
                conn.sendall(mac(b'server')); tx=AESGCM(mac(b'h2c'));rx=AESGCM(mac(b'c2h')); sent=received=0
                def receive():
                    nonlocal received
                    header=read(4); length=struct.unpack('>I',header)[0];assert 17<=length<=65536
                    data=rx.decrypt(b'\0'*4+struct.pack('>Q',received),read(length),header);received+=1
                    return data[0], json.loads(data[1:]) if data[0]==1 else data[1:]
                def send(kind,value):
                    nonlocal sent
                    payload=json.dumps(value,separators=(',',':')).encode() if kind==1 else value
                    header=struct.pack('>I',len(payload)+17)
                    conn.sendall(header+tx.encrypt(b'\0'*4+struct.pack('>Q',sent),bytes([kind])+payload,header));sent+=1
                _,request=receive();assert request['session']=='a'*64 and request['requestId']==1
                if mode=='push':
                    content='Swift file → Windows 🎉'.encode()
                    assert request['op']=='file_push' and request['items']==[{'name':'报告.txt','size':len(content)}]
                    send(1,{'op':'file_ready'});assert receive()==(1,{'op':'file_start','index':0})
                    data=b''
                    while True:
                        kind,value=receive()
                        if kind==3:data+=value
                        else: assert value=={'op':'file_end','index':0,'sha256':hashlib.sha256(content).hexdigest()};break
                    assert data==content and receive()==(1,{'op':'file_complete'})
                    send(1,{'op':'file_done','clipboardReady':True})
                else:
                    content='Python file → Mac 🎉'.encode()
                    assert request['op']=='file_pull' and request['offer']=='b'*32
                    send(1,{'op':'file_ready','items':[{'name':'报告.txt','size':len(content)}]})
                    send(1,{'op':'file_start','index':0});send(3,content)
                    if mode=='cancel': assert conn.recv(1)==b''
                    else:
                        send(1,{'op':'file_end','index':0,'sha256':'0'*64 if mode=='checksum' else hashlib.sha256(content).hexdigest()})
                        send(1,{'op':'file_complete'})
                        if mode=='checksum': assert conn.recv(1)==b''
                        else: assert receive()==(1,{'op':'file_done','clipboardReady':True})
            assert process.wait(timeout=12)==0
        finally:
            listener.close()
            if process.poll() is None: process.kill();process.wait()
for mode in ('push','pull','checksum','cancel'): check(sys.argv[1],mode)
print('PASS independent Python ↔ production Swift file worker; loopback only. Apple SDK/native pasteboard acceptance separate.')
