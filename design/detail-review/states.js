"use strict";
// Offline design states only. No system dialogs, capture, input or transport.
Object.assign(strings.en, {
  captureGuide:"Allow screen recording", inputGuide:"Allow keyboard & mouse control",
  captureSteps:"Open System Settings → Privacy & Security → Screen & System Audio Recording, then allow the installed Local Remote Desktop app.",
  inputSteps:"Open System Settings → Privacy & Security → Accessibility, then allow the installed Local Remote Desktop app.",
  permissionReturn:"After allowing access, return here to check again. If macOS asks you to quit and reopen, follow its prompt.",
  openSettings:"Open System Settings", checkAgain:"Check again", checkedPreview:"Preview: the simulated permission is unchanged. This does not query or change macOS.",
  guide:"View guidance", viewingOnly:"View only", startView:"Start accepting · view only", controlDenied:"The other device has not allowed keyboard and mouse control.",
  retry:"Try again", retryFailed:"Preview: the port-conflict fixture is still active.", connectionFailed:"Could not reach this device. Check its address and whether it is accepting connections.",
  unreachableState:"Device unreachable", reconnectState:"Reconnecting", viewState:"Remote session · view only", sessionState:"Remote session",
  reconnectTitle:"Reconnecting to your device", reconnectHint:"The connection was interrupted. Your remote desktop will return when the device is reachable.",
  cancelReconnect:"Cancel reconnecting", retryNow:"Retry now", recovered:"Preview: connection restored.",
  sessionOptions:"Connection options", sessionHint:"Changes apply to this connection only.", disabledGlobally:"Disabled in Settings", done:"Done",
  expand:"Full screen", collapse:"Exit full screen", expandPreview:"Preview: expanded the canvas. The native app would enter full screen.",
  backHome:"Back to Home", textSession:"Text clipboard", filesSession:"File clipboard", filesPaused:"Preview: file sync disabled; the fictional transfer was cancelled.",
  desktopPlaceholder:"Remote desktop preview", desktopPlaceholderHint:"When connected, the other device’s actual desktop appears here.",
  sessionPreviewLabel:"Fictional remote desktop · no screen capture or input", finishTransfer:"Complete transfer", fileComplete:"File ready", fileCompleteHint:"Preview only. No file was received.", completeState:"File transfer complete"
});
Object.assign(strings.zh, {
  captureGuide:"允许屏幕录制", inputGuide:"允许键盘与鼠标控制",
  captureSteps:"打开“系统设置 → 隐私与安全性 → 屏幕与系统音频录制”，允许已安装的 Local Remote Desktop。",
  inputSteps:"打开“系统设置 → 隐私与安全性 → 辅助功能”，允许已安装的 Local Remote Desktop。",
  permissionReturn:"授权后回到这里重新检查。如果 macOS 提示退出并重新打开，请按系统提示操作。",
  openSettings:"打开系统设置", checkAgain:"重新检查", checkedPreview:"预览：模拟权限未改变，不会读取或修改 macOS 权限。",
  guide:"查看指引", viewingOnly:"仅查看", startView:"开始接受连接 · 仅查看", controlDenied:"对方设备尚未允许键盘与鼠标控制。",
  retry:"重试", retryFailed:"预览：端口占用示例仍然有效。", connectionFailed:"无法连接此设备，请检查地址，并确认对方已开始接受连接。",
  unreachableState:"目标无法连接", reconnectState:"正在重连", viewState:"远程会话 · 仅查看", sessionState:"远程会话",
  reconnectTitle:"正在重新连接设备", reconnectHint:"连接已中断。设备恢复可连接后，将返回远程桌面。",
  cancelReconnect:"取消重连", retryNow:"立即重试", recovered:"预览：已模拟恢复连接。",
  sessionOptions:"连接选项", sessionHint:"仅用于当前连接。", disabledGlobally:"已在设置中关闭", done:"完成",
  expand:"全屏", collapse:"退出全屏", expandPreview:"预览：已展开画面；原生应用将进入全屏。",
  backHome:"返回首页", textSession:"文字剪贴板", filesSession:"文件剪贴板", filesPaused:"预览：已关闭文件同步，并取消虚构传输。",
  desktopPlaceholder:"远程桌面预览", desktopPlaceholderHint:"连接后，这里将显示对方的真实桌面。",
  sessionPreviewLabel:"虚构远程桌面 · 不采集屏幕、不发送输入", finishTransfer:"模拟完成传输", fileComplete:"文件已就绪", fileCompleteHint:"仅为预览，未接收任何文件。", completeState:"文件传输完成"
});
paths.expand='<path d="M9 3H3v6m12-6h6v6M3 15v6h6m6 0h6v-6"/>';
paths.collapse='<path d="M3 9h6V3m6 0v6h6M3 15h6v6m6 0v-6h6"/>';
paths.link='<path d="m9 15 6-6m-5-4 1-1a5 5 0 0 1 7 7l-1 1m-3 7-1 1a5 5 0 0 1-7-7l1-1"/>';
paths.file='<path d="M13 3H6v18h12V8Zm0 0v6h5M9 14h6m-6 3h4"/>';
state.expanded=false;
const specialScenarios={session:"sessionState",view:"viewState",reconnect:"reconnectState",unreachable:"unreachableState",complete:"completeState"};
const previousAccept=accept, previousRender=render, previousModal=renderModal, previousSubmit=submit;

function permissionPanel(kind){return `<div class="guidance-symbol">${icon(kind==="capture"?"monitor":"outgoing")}</div><p class="guide-copy">${t(kind==="capture"?"captureSteps":"inputSteps")}</p><p class="help">${t("permissionReturn")}</p><div class="guide-actions">${btn("permission-settings",t("openSettings"),"primary")}${btn("locate",t("locate"),"small")}</div>`;}
function openGuide(kind){returnFocus=document.activeElement;state.modal={kind:"guide",access:kind};renderModal();$("overlay").querySelector("button")?.focus();}
permissionRow=function(key,missing){return `<div class="permission-row ${missing?"missing":""}">${icon(missing?"alert":"check")}<span>${t(key)}</span>${missing?btn(`guide-${key}`,t("allow"),"small"): `<span class="allowed">${t("allowed")}</span>`}</div>`;};
accept=function(){
  let html=previousAccept();
  if(state.platform==="mac"&&state.scenario==="input"){
    html=html.replace(t("start")+"</button>",t("startView")+"</button>");
    if(state.accepting)html=html.replace(t("waiting"),`${t("waiting")} · ${t("viewingOnly")}`);
  }
  if(state.scenario==="error"){
    const start=html.indexOf('<div class="notice"');
    const end=html.indexOf('</div><div class="action-footer">',start);
    if(start>=0&&end>=0)html=html.slice(0,start)+`<section class="failure-panel" role="alert"><span class="failure-symbol">${icon("alert")}</span><div><strong>${t("startError")}</strong><div class="failure-actions">${btn("details",t("details"),"small",`aria-expanded="${state.details}" aria-controls="failure-detail"`)}${btn("diagnostics",t("diagnostics"),"small")}</div>${state.details?`<div id="failure-detail" class="failure-detail"><p>${t("portDetail")}</p><code>SocketError: AddressAlreadyInUse · 10048</code></div>`:""}</div></section>`+html.slice(end);
    html=html.replace(`data-action="toggle-accept"`,`data-action="retry-listener"`).replace(t("start")+"</button>",t("retry")+"</button>");
  }
  return html;
};

function sessionPolicy(){const peer=state.session||{quality:state.settings.quality};peer.text??=true;peer.files??=true;return peer;}
function sessionToggle(key){const peer=sessionPolicy(),enabled=state.settings[key];return `<div class="setting-row"><div><label for="session-sync-${key}">${t(key+"Session")}</label>${!enabled?`<p class="help">${t("disabledGlobally")}</p>`:""}</div><input id="session-sync-${key}" type="checkbox" ${peer[key]&&enabled?"checked":""} ${enabled?"":"disabled"}></div>`;}
session=function(){
  const peer=sessionPolicy(),reconnecting=state.scenario==="reconnect",viewOnly=state.scenario==="view";
  return `<div class="remote-session"><header class="controlbar">${btn("home",icon("back"),"tool-icon",`aria-label="${t("backHome")}" title="${t("backHome")}"`)}<div class="session-peer"><strong>${escapeHtml(peer.name||peer.ip)}</strong><span><i class="dot"></i>${t(reconnecting?"reconnectState":viewOnly?"viewingOnly":"connected")}</span></div><div class="session-actions">${btn("session-options",icon("gear"),"tool-icon",`aria-label="${t("sessionOptions")}" title="${t("sessionOptions")}"`)}${btn("expand-preview",icon(state.expanded?"collapse":"expand"),"tool-icon",`aria-label="${t(state.expanded?"collapse":"expand")}" title="${t(state.expanded?"collapse":"expand")}"`)}${btn("disconnect",t("disconnect"),"disconnect")}</div></header>${viewOnly?`<div class="view-banner">${icon("alert")}<span>${t("controlDenied")}</span>${btn("guide-view",t("guide"),"small")}</div>`:""}<div class="desktop-canvas"><div class="desktop-placeholder"><span class="desktop-placeholder-symbol">${icon("monitor")}</span><h3>${t("desktopPlaceholder")}</h3><p>${t("desktopPlaceholderHint")}</p></div>${reconnecting?`<section class="reconnect-panel" role="status"><span class="reconnect-symbol">${icon("refresh")}</span><h3>${t("reconnectTitle")}</h3><p>${t("reconnectHint")}</p><div>${btn("disconnect",t("cancelReconnect"),"small")}${btn("retry-session",t("retryNow"),"primary")}</div></section>`:""}</div><p class="session-disclosure">${t("sessionPreviewLabel")}</p>${state.scenario==="transfer"?`<section class="file-notice"><span class="file-symbol">${icon("file")}</span><div><strong>${t("file")}</strong><span>${t("transfer")}</span><progress value="64" max="100" aria-label="${t("transfer")}"></progress></div>${btn("cancel-transfer",t("cancel"),"small")}</section>`:state.scenario==="complete"?`<section class="file-notice complete"><span class="file-symbol">${icon("check")}</span><div><strong>${t("fileComplete")}</strong><span>${t("file")} · ${t("fileCompleteHint")}</span></div>${btn("dismiss-transfer",icon("close"),"tool-icon",`aria-label="${t("close")}"`)}</section>`:""}</div>`;
};

renderModal=function(){
  const modal=state.modal;
  if(!modal||!["guide","session-options"].includes(modal.kind)){previousModal();return;}
  const layer=$("overlay"),isGuide=modal.kind==="guide";
  layer.hidden=false;$("screen").inert=true;$("review-pages").inert=true;
  const viewGuide=modal.access==="view";
  const title=t(isGuide?(modal.access==="capture"?"captureGuide":"inputGuide"):"sessionOptions");
  const body=isGuide?(viewGuide?`<div class="guidance-symbol">${icon("monitor")}</div><p class="guide-copy">${t("controlDenied")}</p><p class="help">${t("inputSteps")}</p>`:permissionPanel(modal.access)):`<p class="help">${t("sessionHint")}</p><div class="setting-row"><label for="session-quality">${t("quality")}</label><select id="session-quality">${qualities(sessionPolicy().quality)}</select></div><section class="preference-section">${sessionToggle("text")}${sessionToggle("files")}</section>`;
  layer.innerHTML=`<section class="sheet" role="dialog" aria-modal="true" aria-labelledby="dialog-title"><header class="sheet-header"><h2 id="dialog-title">${title}</h2>${btn("close",icon("close"),"icon-button",`aria-label="${t("close")}"`)}</header><div class="sheet-body">${body}</div><footer class="sheet-footer">${isGuide&&!viewGuide?btn("permission-check",t("checkAgain"),"primary"):btn("close",t("done"),"primary")}</footer></section>`;
};
submit=function(){if(state.modal?.kind==="target"&&state.scenario==="unreachable"){state.modal.error="connectionFailed";renderModal();$("target-password")?.focus();return;}previousSubmit();};
render=function(){if(state.page==="session"&&!state.session)state.page=state.accepting?"accept":"connect";previousRender();for(const [value,key] of Object.entries(specialScenarios)){const option=document.createElement("option");option.value=value;option.textContent=t(key);option.selected=state.scenario===value;$("scenario").appendChild(option);}$("app").classList.toggle("session-expanded",state.expanded&&state.page==="session");$("app").classList.toggle("listener-failed",state.scenario==="error");};

document.addEventListener("click",event=>{
  const button=event.target.closest("button[data-action]");if(!button||button.disabled)return;
  const action=button.dataset.action;
  if(!["guide-capture","guide-input","guide-view","permission-settings","permission-check","retry-listener","session-options","expand-preview","retry-session","complete-transfer","dismiss-transfer","details"].includes(action))return;
  if(action==="details"&&state.scenario!=="error")return;
  event.preventDefault();event.stopImmediatePropagation();
  if(action.startsWith("guide-"))openGuide(action.slice(6));
  else if(action==="permission-settings")notify("permission");
  else if(action==="permission-check")notify("checkedPreview");
  else if(action==="retry-listener"){state.details=true;render();$("failure-detail")?.scrollIntoView({block:"nearest"});notify("retryFailed");}
  else if(action==="details"){state.details=!state.details;render();$("failure-detail")?.scrollIntoView({block:"nearest"});}
  else if(action==="session-options"){returnFocus=document.activeElement;state.modal={kind:"session-options"};renderModal();$("overlay").querySelector("button")?.focus();}
  else if(action==="expand-preview"){state.expanded=!state.expanded;render();if(state.expanded)notify("expandPreview");}
  else if(action==="retry-session"){state.scenario="session";render();notify("recovered");}
  else if(action==="complete-transfer"){state.scenario="complete";render();}
  else if(action==="dismiss-transfer"){state.scenario="session";render();}
},true);
document.addEventListener("change",event=>{
  const el=event.target;
  if(el.id==="scenario"&&Object.hasOwn(specialScenarios,el.value)){
    event.stopImmediatePropagation();clearTimeout(connectTimer);state.busy=false;state.modal=null;state.accepting=false;state.scenario=el.value;state.expanded=false;
    if(el.value==="unreachable"){state.session=null;state.page="connect";render();openModal("target",peers[0]);state.modal.error="connectionFailed";renderModal();$("target-password").focus();}
    else{state.session={...peers[0],quality:state.settings.quality};state.page="session";render();}
  }else if(el.id.startsWith("session-sync-")){
    event.stopImmediatePropagation();const key=el.id.endsWith("text")?"text":"files";
    sessionPolicy()[key]=state.settings[key]&&el.checked;
    if(key==="files"&&!state.session.files&&state.scenario==="transfer"){state.scenario="session";render();notify("filesPaused");}
  }
},true);
document.addEventListener("keydown",event=>{if(event.key==="Escape"&&!state.modal&&state.expanded){event.preventDefault();state.expanded=false;render();}});
render();
// Allowlisted review links select fictional fixtures, never actual connections.
const requestedState=new URLSearchParams(location.search).get("state");
if([...Object.keys(specialScenarios),"capture","input","error","transfer"].includes(requestedState)){
  $("scenario").value=requestedState;
  $("scenario").dispatchEvent(new Event("change",{bubbles:true}));
}
