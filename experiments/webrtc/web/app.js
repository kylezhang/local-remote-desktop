'use strict';
const byId=id=>document.getElementById(id);
let peer=null,channel=null,timer=null,last=null,records=[],run=0;
const status=text=>{byId('status').textContent=text;};
async function gather(pc){if(pc.iceGatheringState==='complete')return;await new Promise((resolve,reject)=>{const timeout=setTimeout(()=>{pc.removeEventListener('icegatheringstatechange',check);reject(new Error('局域网地址收集超时'));},12000);const check=()=>{if(pc.iceGatheringState==='complete'){clearTimeout(timeout);pc.removeEventListener('icegatheringstatechange',check);resolve();}};pc.addEventListener('icegatheringstatechange',check);check();});}
function stop(){run++;clearInterval(timer);timer=null;if(peer)peer.close();peer=null;channel=null;byId('desktop').srcObject=null;byId('start').disabled=false;status('已断开');}
byId('stop').onclick=stop;
byId('connect').onsubmit=async event=>{
 event.preventDefault();stop();const session=run;records=[];last=null;byId('stats').textContent='等待视频指标…';byId('start').disabled=true;
 const pc=new RTCPeerConnection({iceServers:[],bundlePolicy:'max-bundle'});peer=pc;
 const transceiver=pc.addTransceiver('video',{direction:'recvonly'});
 const codecs=RTCRtpReceiver.getCapabilities('video')?.codecs.filter(c=>c.mimeType.toLowerCase()==='video/h264');
 if(!codecs?.length){stop();status('此浏览器未提供 H.264 WebRTC 解码器');return;}
 transceiver.setCodecPreferences(codecs);
 // Reserved diagnostic channel; no input commands are sent. RTT comes from ICE statistics.
 channel=pc.createDataChannel('diagnostic',{ordered:true});
 pc.ontrack=event=>{if(session!==run)return;byId('desktop').srcObject=event.streams[0]||new MediaStream([event.track]);};
 pc.onconnectionstatechange=()=>{if(session!==run)return;status('WebRTC '+pc.connectionState+' · 只读画面');if(['failed','closed'].includes(pc.connectionState)){byId('start').disabled=false;}};
 try{
  status('收集局域网连接信息…');await pc.setLocalDescription(await pc.createOffer());await gather(pc);
  if(session!==run)return;status('验证 Mac 并协商视频…');
  const response=await fetch('/signal',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({host:byId('host').value.trim(),key:byId('key').value.trim(),offer:pc.localDescription})});
  if(!response.ok)throw new Error(await response.text());const answer=await response.json();if(session!==run)return;await pc.setRemoteDescription(answer);
  byId('key').value='';timer=setInterval(()=>metrics(pc,session).catch(error=>{if(session===run)status('统计失败：'+error.message);}),1000);
 }catch(error){if(session===run){stop();status('连接失败：'+error.message);}}
};
async function metrics(pc,session){
 const report=await pc.getStats();if(session!==run)return;let inbound,selected,codec;
 report.forEach(stat=>{if(stat.type==='inbound-rtp'&&stat.kind==='video')inbound=stat;if(stat.type==='transport'&&stat.selectedCandidatePairId)selected=report.get(stat.selectedCandidatePairId);});
 if(!inbound){byId('stats').textContent='已协商，等待第一帧。如果一直没有画面，查看 Mac 桥接终端的 video source 错误。';return;}
 codec=report.get(inbound.codecId);const now=performance.now(),seconds=last?(now-last.time)/1000:0;
 const frames=inbound.framesDecoded||0,bytes=inbound.bytesReceived||0,deltaFrames=last?frames-last.frames:0;
 const decodeDelta=last?(inbound.totalDecodeTime||0)-last.decode:0;
 const emitted=inbound.jitterBufferEmittedCount||0,emitDelta=last?emitted-last.emitted:0;
 const jitterDelta=last?(inbound.jitterBufferDelay||0)-last.jitterDelay:0;
 const local=selected?report.get(selected.localCandidateId):null,remote=selected?report.get(selected.remoteCandidateId):null;
 const value={elapsedSeconds:records.length+1,codec:codec?.mimeType||null,width:inbound.frameWidth||null,height:inbound.frameHeight||null,decodedFrames:frames,fps:seconds?deltaFrames/seconds:null,mbps:seconds?(bytes-last.bytes)*8/seconds/1e6:null,decodeMs:deltaFrames>0?decodeDelta*1000/deltaFrames:null,jitterBufferMs:emitDelta>0?jitterDelta*1000/emitDelta:null,rttMs:selected?.currentRoundTripTime!=null?selected.currentRoundTripTime*1000:null,packetsLost:inbound.packetsLost??null,framesDropped:inbound.framesDropped??null,candidateTypes:[local?.candidateType||null,remote?.candidateType||null],protocol:local?.protocol||null};
 records.push(value);if(records.length>600)records.shift();window.rtcExperimentStats=value;
 const f=(v,n=1)=>v==null?'暂无':v.toFixed(n);
 byId('stats').textContent=`${value.width||'?'} × ${value.height||'?'} | ${value.codec}\n解码帧数 ${frames} | FPS ${f(value.fps)} | 接收 ${f(value.mbps,2)} Mbps\n平均解码 ${f(value.decodeMs)} ms | 抖动缓冲 ${f(value.jitterBufferMs)} ms | 网络 RTT ${f(value.rttMs)} ms\n丢包 ${value.packetsLost} | 丢帧 ${value.framesDropped} | ICE ${value.candidateTypes.join(' ↔ ')} / ${value.protocol}`;
 last={time:now,frames,bytes,decode:inbound.totalDecodeTime||0,emitted,jitterDelay:inbound.jitterBufferDelay||0};
}
byId('export').onclick=()=>{const blob=new Blob([JSON.stringify({experiment:'remote-mac-lan-webrtc-view-only',createdAt:new Date().toISOString(),samples:records},null,2)],{type:'application/json'});const url=URL.createObjectURL(blob),a=document.createElement('a');a.href=url;a.download='webrtc-metrics.json';a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);};
window.addEventListener('beforeunload',()=>peer?.close());
