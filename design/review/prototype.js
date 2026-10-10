'use strict';
// Offline design review: all devices, credentials, files and responses are fictional.
// No fetch, socket, OS clipboard, persistence or platform permission APIs are used.
const $ = selector => document.querySelector(selector);
const esc = value => String(value).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const texts = {
  en: {
    review:'Revision 2 · Flow, settings and native visual detail · Awaiting review',
    sample:'Fictional devices and content. Interactions are simulated; no connection, clipboard or transfer occurs. Mobile and Linux are future design references.',
    connect:'Connect',share:'Share this device',session:'Remote desktop',settings:'Settings',platform:'Platform',language:'Language',preview:'Design preview',
    intro:'Choose the device you want to connect to and control.',nearby:'Devices',manual:'Enter Target IP',refresh:'Refresh devices',remembered:'Password saved',newDevice:'First connection',
    ready:'Sharing available',offline:'Offline',targetIP:'Target IP',access:'Access',desktopControl:'Screen & control',viewOnly:'View only',
    password:'Pairing password',enterPassword:'Enter the password shown on the other device.',remember:'Remember this device securely',returnHint:'Your saved password will be used.',
    forget:'Forget saved password',forgetHint:'This device stays in the list. Its saved password is removed; the next connection requires it again.',forgetConfirm:'Forget password',
    emptyTitle:'No devices found',emptyHint:'Start sharing on the other device, then refresh. You can also enter its IP.',offlineHint:'Start sharing on this device, then refresh.',
    discoveredInfo:'Nearby device',knownInfo:'Previously connected device',unknownTarget:'Device information will appear after connecting.',unknownSystem:'Unknown system',manualHint:'Enter the IP and pairing password shown on the target device.',wrong:'The password is incorrect. Check it on the target device and try again.',ipError:'Enter a private or link-local IPv4 address.',
    shareIntro:'Let another device connect to and control your current screen.',off:'Sharing is off',on:'Ready for connections',inUse:'Office PC is connected',
    shareOffHint:'Only paired devices can connect after you start sharing.',shareOnHint:'Your current desktop is available to paired devices.',inUseHint:'Connected at 10:24 · Screen & control',
    start:'Start sharing',stop:'Stop sharing',connectionDetails:'Allow another device to connect',deviceName:'This device’s name',thisIP:'This device’s IP',
    show:'Show password',hide:'Hide password',copy:'Copy password',edit:'Change…',editTitle:'Change pairing password',newPassword:'New password',
    editHint:'Use the password below to connect to this device.',passwordHint:'8 characters, without spaces.',
    editEffect:'Saving stops sharing and disconnects the current device. Start sharing again to use the new password.',save:'Save password',cancel:'Cancel',generate:'Generate',
    editValidate:'Enter exactly 8 characters without spaces.',unchanged:'Enter a different password.',savedToast:'Password changed. Sharing is off.',
    permissions:'Sharing permissions',screen:'Screen access',screenHint:'Required to show this screen.',input:'Keyboard & mouse',inputHint:'Required to allow remote control.',
    granted:'Allowed',needed:'Action needed',openSettings:'Open settings',permHint:'Allow screen access in System Settings, then reopen the app. Your password is unchanged.',
    viewHint:'Screen sharing is available. Allow keyboard & mouse access to enable control.',
    general:'General',connection:'Connection',clipboard:'Clipboard',about:'About',
    appearance:'Appearance',languageHint:'Changes labels without interrupting a connection.',theme:'Theme',themeHint:'Use the system appearance, or choose your own.',system:'System',light:'Light',dark:'Dark',
    defaults:'Connection defaults',defaultHint:'Used for the next connection. Change the active session from its toolbar.',quality:'Picture quality',qualityHint:'Balance detail and responsiveness.',smooth:'Smooth',standard:'Standard',high:'High detail',
    reconnect:'Reconnect automatically',reconnectHint:'Retry an interrupted connection; never retry after you disconnect.',shortcuts:'Shortcut mapping',shortcutHint:'Match familiar copy, paste and selection shortcuts to the target system.',auto:'Match target system',native:'Send keys unchanged',
    clipboardDefaults:'Clipboard sharing',clipboardHint:'Allow sharing in paired sessions, in either role. Turning an option off stops its active sync. Both devices must allow it.',text:'Sync text',textHint:'Newly copied text is shared while connected.',files:'Sync files',filesHint:'Copied regular files transfer while connected. No extra pairing step.',
    receiveFolder:'Received files',receiveHint:'Local copies remain here after the connection ends.',openFolder:'Open folder',
    version:'Development preview · v0.3.0-alpha.2',license:'License',diagnostics:'Troubleshooting',diagnosticHint:'Connection details for investigating a problem.',showDetails:'Show details',hideDetails:'Hide details',copyDiag:'Copy diagnostics',
    diagPrivacy:'The proposed report excludes passwords, clipboard contents and file names. Review it before sharing.',
    connected:'Connected',disconnect:'Disconnect',full:'Full screen',more:'Session options',sessionOnly:'This session',sessionHint:'Changes here apply only to this connection.',release:'Release keyboard & mouse',
    transfer:'Sending Project brief.pdf',transferHint:'6.8 MB of 10 MB · Clipboard transfer',cancelTransfer:'Cancel transfer',cancelled:'Transfer cancelled. The desktop stays connected.',
    reconnecting:'Reconnecting…',reconnectMsg:'The connection was interrupted. The picture is paused while we retry.',cancelReconnect:'Cancel & disconnect',
    shareConflict:'This device is currently sharing. Stop sharing before connecting elsewhere.',stopConnect:'Stop sharing & connect',
    stopConfirm:'Stop sharing and connect?',stopExplain:'The current sharing session will end. This device will then connect to the selected target.',
    copied:'Copy simulated in this design preview.',simulation:'This is a design preview. No system action was performed.',
    normal:'Default',first:'First connection',failed:'Wrong password',manualScenario:'Manual address',empty:'No devices',offlineScenario:'Saved device offline',
    permission:'Screen permission missing',view:'View-only access',inUseScenario:'Device connected',role:'Sharing while connecting',transferScenario:'File transfer',reconnectScenario:'Reconnect',
    scenario:'Preview state',spec:'Design specification',phone:'Travel phone',phoneIntro:'Share your screen with another device.',mobileHint:'Future view-only design. System confirmation is required for each sharing session.',
    touchpad:'Touchpad',direct:'Direct touch',keyButton:'Keyboard',touchHint:'Move here to point. Tap to click.',directHint:'Touch the screen. Pinch to zoom.',phoneScale:'The desktop keeps its original proportions.',
    appName:'Local Remote Desktop',sharingIndicator:'Sharing is on',viewSharing:'Stop sharing',transferOff:'No transfers. Clipboard sync is off for this session.'
  },
  zh: {
    review:'第二轮设计 · 流程、设置与原生视觉细节 · 待审阅',
    sample:'设备、密码、文件和画面均为虚构示例。交互仅演示，不会连接、同步剪贴板或传输。手机与 Linux 是未来平台设计参考。',
    connect:'连接',share:'共享此设备',session:'远程桌面',settings:'设置',platform:'平台',language:'语言',preview:'设计预览',
    intro:'选择你要连接并操作的设备。',nearby:'设备',manual:'输入目标IP',refresh:'刷新设备',remembered:'已记住密码',newDevice:'首次连接',
    ready:'已开启共享',offline:'离线',targetIP:'目标IP',access:'访问能力',desktopControl:'画面与控制',viewOnly:'仅查看',
    password:'配对密码',enterPassword:'输入另一台设备上显示的配对密码。',remember:'安全记住此设备',returnHint:'将使用已保存的密码。',
    forget:'忘记已保存的密码',forgetHint:'设备仍保留在列表中。移除保存的密码后，下次连接需要重新输入。',forgetConfirm:'忘记密码',
    emptyTitle:'没有找到设备',emptyHint:'在另一台设备上开启共享后刷新，也可以直接输入它的 IP。',offlineHint:'在这台设备上开启共享后刷新。',
    discoveredInfo:'已发现的设备',knownInfo:'已连接的设备',unknownTarget:'连接成功后显示设备信息。',unknownSystem:'未知系统',manualHint:'输入目标设备上显示的 IP 和配对密码。',wrong:'密码不正确。请在目标设备上核对后重试。',ipError:'请输入局域网或链路本地 IPv4 地址。',
    shareIntro:'让另一台设备连接并操作你的当前屏幕。',off:'尚未共享',on:'等待连接',inUse:'Office PC 已连接',
    shareOffHint:'开始共享后，配对设备才能连接。',shareOnHint:'配对设备可以连接你的当前桌面。',inUseHint:'10:24 建立连接 · 画面与控制',
    start:'开始共享',stop:'停止共享',connectionDetails:'被另一台设备来连接',deviceName:'本机名称',thisIP:'本机 IP',
    show:'显示密码',hide:'隐藏密码',copy:'复制密码',edit:'修改…',editTitle:'修改配对密码',newPassword:'新密码',
    editHint:'连接本设备时，使用下方密码。',passwordHint:'8 位，不含空格。',
    editEffect:'保存会停止共享并断开当前设备。再次开始共享后，新密码生效。',save:'保存密码',cancel:'取消',generate:'自动生成',
    editValidate:'请输入 8 位且不含空格的密码。',unchanged:'请输入与当前不同的密码。',savedToast:'密码已修改，共享已停止。',
    permissions:'共享权限',screen:'屏幕访问',screenHint:'用于共享当前屏幕。',input:'键盘与鼠标',inputHint:'用于允许远程操作。',
    granted:'已允许',needed:'需要处理',openSettings:'打开设置',permHint:'请在系统设置允许屏幕访问后退出并重开应用。配对密码不会变化。',
    viewHint:'可以共享画面。允许键盘与鼠标访问后，才能远程操作。',
    general:'通用',connection:'连接',clipboard:'剪贴板',about:'关于',
    appearance:'外观',languageHint:'切换界面文字，不会中断连接。',theme:'主题',themeHint:'跟随系统外观，或自行选择。',system:'跟随系统',light:'浅色',dark:'深色',
    defaults:'连接默认选项',defaultHint:'用于下一次连接。当前会话可在工具栏中调整。',quality:'画质',qualityHint:'平衡画面细节与操作流畅度。',smooth:'流畅',standard:'标准',high:'高清',
    reconnect:'自动重连',reconnectHint:'连接意外中断时重试；主动断开后不再重连。',shortcuts:'快捷键映射',shortcutHint:'根据目标系统匹配复制、粘贴、全选等常用快捷键。',auto:'匹配目标系统',native:'原样发送按键',
    clipboardDefaults:'剪贴板共享',clipboardHint:'允许在配对会话的连接与共享两种角色中同步。关闭选项会立即停止相应同步，需要两端均允许。',text:'同步文字',textHint:'连接期间共享新复制的文字。',files:'同步文件',filesHint:'连接期间传输复制的普通文件，无需再次配对。',
    receiveFolder:'接收的文件',receiveHint:'连接结束后，本机文件副本仍保留在此目录。',openFolder:'打开目录',
    version:'开发预览 · v0.3.0-alpha.2',license:'许可证',diagnostics:'问题排查',diagnosticHint:'出现问题时查看连接详情。',showDetails:'查看详情',hideDetails:'收起详情',copyDiag:'复制诊断',
    diagPrivacy:'拟定报告不包含密码、剪贴板内容或文件名；分享前可先检查。',
    connected:'已连接',disconnect:'断开',full:'全屏',more:'会话选项',sessionOnly:'本次会话',sessionHint:'这里的修改只对当前连接生效。',release:'释放键盘与鼠标',
    transfer:'正在发送 Project brief.pdf',transferHint:'6.8 MB / 10 MB · 剪贴板传输',cancelTransfer:'取消传输',cancelled:'传输已取消，桌面连接仍保留。',
    reconnecting:'正在重连…',reconnectMsg:'连接意外中断，重试期间画面已暂停。',cancelReconnect:'取消并断开',
    shareConflict:'本机正在共享，连接其他设备前需要先停止。',stopConnect:'停止共享并连接',
    stopConfirm:'停止共享并连接？',stopExplain:'当前共享会话将结束，本机随后连接所选设备。',
    copied:'设计预览已模拟复制。',simulation:'这是设计预览，未执行系统操作。',
    normal:'默认',first:'首次连接',failed:'密码错误',manualScenario:'手动地址',empty:'没有设备',offlineScenario:'已记住设备离线',
    permission:'缺少屏幕权限',view:'仅查看',inUseScenario:'设备已连接',role:'本机共享中再连接',transferScenario:'文件传输',reconnectScenario:'重连',
    scenario:'预览状态',spec:'设计规范',phone:'Travel phone',phoneIntro:'将你的屏幕共享给另一台设备。',mobileHint:'未来仅查看方案。每次共享都需系统确认。',
    touchpad:'触控板',direct:'直接触控',keyButton:'键盘',touchHint:'在这里移动指针，轻点单击。',directHint:'直接触控画面，双指缩放。',phoneScale:'桌面保持原始比例。',
    appName:'Local Remote Desktop',sharingIndicator:'正在共享',viewSharing:'停止共享',transferOff:'没有传输，本次会话的剪贴板同步已关闭。'
  }
};
const symbols={
  connect:'<rect x="3" y="4" width="18" height="12" rx="2"/><path d="M8 21h8M12 16v5"/>',
  share:'<path d="M4 8h16m-4-4 4 4-4 4M20 16H4m4-4-4 4 4 4"/>',
  settings:'<path d="m10 3-.6 2.2-2 .9-2-.6-2 3.4 1.5 1.6v2.3l-1.5 1.6 2 3.4 2-.6 2 .9.6 2.2h4l.6-2.2 2-.9 2 .6 2-3.4-1.5-1.6v-2.3l1.5-1.6-2-3.4-2 .6-2-.9L14 3z"/><circle cx="12" cy="12" r="3"/>',
  refresh:'<path d="M20 8a8 8 0 1 0 0 8M20 3v5h-5"/>',
  arrow:'<path d="m9 5 7 7-7 7"/>',plus:'<path d="M12 5v14M5 12h14"/>',
  eye:'<path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7S2 12 2 12Z"/><circle cx="12" cy="12" r="3"/>',
  copy:'<rect x="8" y="8" width="12" height="13" rx="2"/><path d="M16 8V3H3v13h5"/>',
  screen:'<rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/>',
  laptop:'<rect x="5" y="3" width="14" height="12" rx="1.5"/><path d="M5 15 2 20h20l-3-5"/>',
  phone:'<rect x="6" y="2" width="12" height="20" rx="3"/><path d="M10 18h4"/>',
  shield:'<path d="m12 2 8 4v6c0 5-8 10-8 10s-8-5-8-10V6z"/><path d="m8 12 3 3 5-6"/>',
  check:'<path d="m5 12 4 4L19 6"/>',warning:'<path d="m12 3 10 18H2zM12 9v5m0 3v.1"/>',
  keyboard:'<rect x="2" y="5" width="20" height="14" rx="2"/><path d="M5 9h1m3 0h1m3 0h1m3 0h1M5 12h1m3 0h1m3 0h1m3 0h1M7 16h10"/>',
  full:'<path d="M9 3H3v6m12-6h6v6M3 15v6h6m6 0h6v-6"/>',more:'<circle cx="5" cy="12" r="1"/><circle cx="12" cy="12" r="1"/><circle cx="19" cy="12" r="1"/>',
  folder:'<path d="M3 6h7l2 3h9v12H3z"/>',moon:'<path d="M20 15a8 8 0 0 1-11-11 9 9 0 1 0 11 11Z"/>',
  pointer:'<path d="m5 3 15 11-7 1 4 7-3 2-4-8-5 5z"/>',info:'<circle cx="12" cy="12" r="9"/><path d="M12 10v7m0-10v.1"/>'
};
const icon=name=>`<svg class="icon" viewBox="0 0 24 24" aria-hidden="true">${symbols[name]||symbols.screen}</svg>`;
const t=key=>texts[state.lang][key]??key;
const mobile=()=>['android','ios','harmony'].includes(state.platform);
const logo='../../assets/brand/icon.svg';
const devices=[
  {name:'Studio Mac',os:'macOS',ip:'192.168.1.24',source:'discovery',saved:true,online:true,kind:'laptop'},
  {name:'Office PC',os:'Windows',ip:'192.168.1.38',source:'discovery',saved:false,online:true,kind:'screen'},
  {name:'Travel Mac',os:'macOS',ip:'192.168.1.51',source:'discovery',saved:false,online:true,kind:'laptop'}
];
// Authenticated responses are separate fixtures, not inferred from an IP address.
const peerFixtures = new Map([...devices, {name:'Meeting PC',os:'Windows',ip:'192.168.1.70',kind:'screen'}].map(d=>[d.ip,{name:d.name,os:d.os,kind:d.kind}]));
const state={page:'connect',platform:'windows',lang:'en',scenario:'normal',selected:0,sharing:false,inUse:false,reveal:false,
  password:'Blue482X',dialog:null,error:'',draft:'',draftIP:'',remember:true,inputReveal:false,options:false,keyboard:false,touch:'touchpad',fullscreen:false,
  settingTab:'connection',diagnostics:false,preferences:{text:true,files:true,reconnect:true,shortcuts:'auto',quality:'standard',theme:'system'},
  session:{text:true,files:true,shortcuts:'auto',quality:'standard'},transfer:true};
const button=(label,action,cl='',symbol='',disabled=false)=>`<button class="${cl}" data-action="${action}" ${disabled?'disabled':''}>${symbol?icon(symbol):''}${esc(label)}</button>`;
const iconButton=(name,action,label)=>`<button class="quiet icon-button" data-action="${action}" aria-label="${esc(label)}" title="${esc(label)}">${icon(name)}</button>`;
const choice=(id,value,keys)=>`<select id="${id}" aria-label="${esc(t(id==='defaultQuality'||id==='quality'?'quality':id==='themePref'?'theme':'shortcuts'))}">${keys.map(k=>`<option value="${k}" ${value===k?'selected':''}>${esc(t(k))}</option>`).join('')}</select>`;
function chrome(){return mobile()?'<div class="chrome"><span>9:41</span><span aria-hidden="true">▰ 100%</span></div>':`<div class="chrome"><div class="traffic"><i></i><i></i><i></i></div><img class="brand" src="${logo}" alt=""><span>Local Remote Desktop</span><span class="window-actions" aria-hidden="true"><span>−</span><span>□</span><span>×</span></span></div>`;}
function navButton(page,symbol){return `<button class="nav ${state.page===page?'active':''}" data-page="${page}" aria-current="${state.page===page?'page':'false'}">${icon(symbol)}<span>${t(page)}</span></button>`;}
function navigation(){return `<aside class="rail"><div class="rail-caption">${state.platform==='mac'?'Design Mac':'Desk PC'}</div>${navButton('connect','connect')}${navButton('share','share')}<div class="rail-bottom">${state.sharing?`<div class="rail-status"><span class="dot"></span>${t('sharingIndicator')}</div>`:''}${navButton('settings','settings')}</div></aside>`;}
function bottom(){return `<nav class="bottom-nav" aria-label="${t('appName')}">${['connect','share','settings'].map(p=>`<button data-page="${p}" class="${state.page===p?'active':''}">${icon(p==='connect'?'connect':p==='share'?'share':'settings')}${t(p)}</button>`).join('')}</nav>`;}
function heading(title,hint='',action=''){return `<div class="page-heading"><div><h2>${t(title)}</h2>${hint?`<p>${t(hint)}</p>`:''}</div>${action}</div>`;}
function passwordField(value,id='connectPassword'){return `<div class="field"><label for="${id}">${t('password')}</label><div class="password-input"><input id="${id}" type="${state.inputReveal?'text':'password'}" value="${esc(value)}" autocomplete="off" aria-describedby="pairHint">${iconButton('eye','inputReveal',t(state.inputReveal?'hide':'show'))}</div></div>`;}
function deviceOS(d){return d.os||t('unknownSystem');}
function manualIdentity(){
  const ip=state.draftIP.trim(),d=devices.find(x=>x.ip===ip);
  if(!ip)return '';
  return d?`<div class="manual-peer"><span class="device-symbol">${icon(d.kind)}</span><div><strong>${esc(d.name)}</strong><span>${esc(deviceOS(d))} · ${t(d.source==='discovery'?'discoveredInfo':'knownInfo')}</span></div></div>`:`<p class="hint">${t('unknownTarget')}</p>`;
}
function updateManualIdentity(){const el=$('#manualIdentity');if(el)el.innerHTML=manualIdentity();}
function rememberField(){return `<label class="check"><input id="remember" type="checkbox" ${state.remember?'checked':''}>${t('remember')}</label>`;}
function connect(){const d=devices[state.selected],offline=state.scenario==='offlineScenario';return heading('connect','intro',button(mobile()?'IP':t('manual'),'manual','quiet','plus'))+`<div class="connect-layout"><div><div class="section-title"><span>${t('nearby')}</span>${iconButton('refresh','refresh',t('refresh'))}</div><div class="device-list">${state.scenario==='empty'?`<div class="empty">${icon('connect')}<h3>${t('emptyTitle')}</h3><p>${t('emptyHint')}</p>${button(t('manual'),'manual')}</div>`:devices.map((x,i)=>`<button class="device ${i===state.selected?'selected':''} ${offline&&i===0?'offline':''}" data-device="${i}" aria-current="${i===state.selected?'true':'false'}"><span class="device-symbol">${icon(x.kind)}</span><span class="device-copy"><span class="device-title">${esc(x.name)}</span><span class="device-meta">${esc(deviceOS(x))}<span>·</span>${t(x.saved?'remembered':'newDevice')}${offline&&i===0?`<span>· ${t('offline')}</span>`:''}</span></span>${icon('arrow')}</button>`).join('')}</div></div>${state.scenario==='empty'?'':`<aside class="detail"><div class="detail-symbol">${icon(d.kind)}</div><h3>${esc(d.name)}</h3><p class="platform-name">${esc(deviceOS(d))}</p><div class="status ${offline?'muted':''}">${offline?'':`<span class="dot"></span>`}${t(offline?'offline':'ready')}</div><div class="meta-pairs"><span>${t('targetIP')}</span><span class="mono">${esc(d.ip)}</span><span>${t('access')}</span><span>${t('desktopControl')}</span></div>${d.saved?`<p class="hint">${t('returnHint')}</p>`:`<p class="hint" id="pairHint">${t('enterPassword')}</p>${passwordField(state.draft)}${rememberField()}`}${state.error&&!state.dialog?`<p class="inline-error" role="alert">${esc(state.error)}</p>`:''}${button(t('connect'),'connect','primary','',offline)}${d.saved?button(t('forget'),'forget','quiet small'):''}${offline?`<p class="hint">${t('offlineHint')}</p>`:''}${state.sharing?`<div class="role-note">${t('shareConflict')}${button(t('share'),'goShare','quiet')}</div>`:''}</aside>`}</div>`;}
function permissionRow(title,hint,symbol,missing){return `<div class="permission-row">${icon(symbol)}<div class="permission-copy"><h4>${t(title)}</h4><p>${t(hint)}</p></div>${missing?button(t('openSettings'),'permissions','quiet'): `<span class="permission-state">${icon('check')}${t('granted')}</span>`}</div>`;}
function share(){const mac=state.platform==='mac',phone=mobile(),missing=state.scenario==='permission';return heading('share',phone?'phoneIntro':'shareIntro')+`<div class="sharing">${missing||state.scenario==='view'?`<div class="callout">${icon('warning')}<span>${t(missing?'permHint':'viewHint')}</span></div>`:''}<div class="share-state"><div class="device-symbol">${icon(phone?'phone':'share')}</div><div class="state-copy"><h3>${t(state.inUse?'inUse':state.sharing?'on':'off')}</h3><p>${t(state.inUse?'inUseHint':state.sharing?'shareOnHint':'shareOffHint')}</p></div>${button(t(state.sharing?'stop':'start'),'sharing',state.sharing?'danger':'primary')}</div><section class="connection-info"><h4>${t('connectionDetails')}</h4><div class="address-grid"><span>${t('deviceName')}</span><span>${phone?t('phone'):mac?'Design Mac':'Desk PC'}</span><span>${t('thisIP')}</span><span class="mono">192.168.1.${phone?'62':mac?'16':'18'}</span></div></section><div class="label-row"><div class="row-copy"><h4>${t('password')}</h4><p class="password-value">${state.reveal?esc(state.password):'••••••••'}</p></div><div class="row-actions">${iconButton('eye','reveal',t(state.reveal?'hide':'show'))}${iconButton('copy','copy',t('copy'))}${button(t('edit'),'edit','quiet')}</div></div>${mac?`<section class="permission-section"><h4>${t('permissions')}</h4>${permissionRow('screen','screenHint','screen',missing)}${permissionRow('input','inputHint','keyboard',state.scenario==='view')}</section>`:''}${phone?`<div class="label-row"><div class="row-copy"><h4>${t('access')}</h4><p>${t('mobileHint')}</p></div><span class="small muted">${t('viewOnly')}</span></div>`:''}</div>`;}
function toggle(key,scope='preferences'){const allowed=scope!=='session'||state.preferences[key];return `<button class="toggle ${allowed&&state[scope][key]?'on':''}" role="switch" aria-checked="${Boolean(allowed&&state[scope][key])}" aria-label="${esc(t(key))}" data-toggle="${key}" data-scope="${scope}" ${allowed?'':'disabled'}></button>`;}
function row(title,hint,control){return `<div class="label-row setting-row"><div class="row-copy"><h4>${t(title)}</h4>${hint?`<p>${t(hint)}</p>`:''}</div>${control}</div>`;}
function languageChoice(){return `<select id="appLang" aria-label="${t('language')}"><option value="en" ${state.lang==='en'?'selected':''}>English</option><option value="zh" ${state.lang==='zh'?'selected':''}>简体中文</option></select>`;}
function settings(){let body='';switch(state.settingTab){
  case 'general':body=`<section class="settings-group"><h3>${t('appearance')}</h3>${row('language','languageHint',languageChoice())}${row('theme','themeHint',choice('themePref',state.preferences.theme,['system','light','dark']))}</section>`;break;
  case 'connection':body=`<section class="settings-group"><h3>${t('defaults')}</h3><p class="hint">${t('defaultHint')}</p>${row('quality','qualityHint',choice('defaultQuality',state.preferences.quality,['smooth','standard','high']))}${row('reconnect','reconnectHint',toggle('reconnect'))}${row('shortcuts','shortcutHint',choice('defaultShortcuts',state.preferences.shortcuts,['auto','native']))}</section>`;break;
  case 'clipboard':body=`<section class="settings-group"><h3>${t('clipboardDefaults')}</h3><p class="hint">${t('clipboardHint')}</p>${row('text','textHint',toggle('text'))}${row('files','filesHint',toggle('files'))}</section><section class="settings-group"><h3>${t('receiveFolder')}</h3><p class="hint">${t('receiveHint')}</p><div class="storage-path"><span>${state.platform==='windows'?'Downloads\\Local Remote Desktop':'Downloads/Local Remote Desktop'}</span>${button(t('openFolder'),'folder','quiet','folder')}</div></section>`;break;
  case 'about':body=`<div class="about-heading"><img src="${logo}" alt=""><div><h3>Local Remote Desktop</h3><p>${t('version')}</p></div></div>${row('license','','<span class="small">GPL-3.0-or-later</span>')}${row('diagnostics','diagnosticHint',button(t(state.diagnostics?'hideDetails':'showDetails'),'diagnostics','quiet'))}${state.diagnostics?`<div class="diagnostics-detail"><p>${t('diagPrivacy')}</p><pre>Connection: not connected\nSharing: ${state.sharing?'available':'off'}\nReport: fictional preview\nApplication: ${state.platform==='windows'?'LocalRemoteDesktop.exe':state.platform==='mac'?'Local Remote Desktop.app':'Local Remote Desktop'}</pre>${button(t('copyDiag'),'copy','quiet','copy')}</div>`:''}`;break;
}return heading('settings')+`<div class="settings-tabs" role="tablist" aria-label="${t('settings')}">${['general','connection','clipboard','about'].map(k=>`<button role="tab" aria-selected="${state.settingTab===k}" data-setting-tab="${k}">${t(k)}</button>`).join('')}</div><div class="settings-body" role="tabpanel">${body}</div>`;}
function scene(){return `<div class="remote-scene" aria-label="Fictional remote screen"><div class="desktop-menu"><b>◈</b><strong>Notes</strong><span>File</span><span>Edit</span><span>View</span><time>10:24</time></div><div class="remote-wallpaper"></div><div class="remote-window"><div class="remote-window-title"><i></i><i></i><i></i><span>Project notes</span></div><div class="remote-work"><div class="remote-sidebar"><p>All notes</p><p>Project notes</p><p>Ideas</p><p>Archive</p></div><article class="remote-note"><h2>A little less switching.</h2><div class="note-date">Today at 10:24</div><p>The same desk. The same work.<br>One place to reach the other screen.</p><div class="checkline">Prepare the project outline</div><div class="checkline">Review the latest draft</div><p>Next: finish the presentation.</p></article></div></div><div class="dock" aria-hidden="true"><i>▣</i><i>◉</i><i>▤</i><i>⌘</i><i>⚙</i></div></div>`;}
function viewer(){return `<div class="viewer"><div class="sessionbar"><span class="dot"></span><span class="session-target">${esc(devices[state.selected].name)}</span><span class="session-state">${t(state.scenario==='reconnectScenario'?'reconnecting':'connected')}</span><span class="spacer"></span><span class="quality">${choice('quality',state.session.quality,['smooth','standard','high'])}</span><span class="full">${iconButton('full','fullscreen',t('full'))}</span>${iconButton('more','options',t('more'))}${button(t('disconnect'),'disconnect','quiet danger')}</div><div class="desktop">${scene()}${mobile()?`<div class="touch-guide">${icon('pointer')}<span>${t('phoneScale')}</span><span>${t(state.touch==='touchpad'?'touchHint':'directHint')}</span></div>`:''}</div>${state.scenario==='transferScenario'&&state.transfer?`<aside class="transfer"><div class="transfer-head"><span>${t('transfer')}</span>${button(t('cancelTransfer'),'cancelTransfer','quiet')}</div><p>${t('transferHint')}</p><div class="progress" role="progressbar" aria-label="${t('transfer')}" aria-valuenow="68" aria-valuemin="0" aria-valuemax="100"><span></span></div></aside>`:''}${state.scenario==='reconnectScenario'?`<div class="reconnect"><div><h3>${t('reconnecting')}</h3><p>${t('reconnectMsg')}</p>${button(t('cancelReconnect'),'disconnect')}</div></div>`:''}<div class="mobile-dock">${button(t(state.touch==='touchpad'?'touchpad':'direct'),'touch','selected','pointer')}${button(t('keyButton'),'keyboard','','keyboard')}${iconButton('more','options',t('more'))}</div>${state.options?`<section class="session-options"><h4>${t('sessionOnly')}</h4><p class="hint">${t('sessionHint')}</p>${mobile()?row('quality','',choice('quality',state.session.quality,['smooth','standard','high'])):''}${row('text','',toggle('text','session'))}${row('files','',toggle('files','session'))}${row('shortcuts','',choice('sessionShortcuts',state.session.shortcuts,['auto','native']))}${button(t('release'),'release','quiet','keyboard')}</section>`:''}${state.keyboard&&mobile()?`<div class="keyboard-preview">${'QWERTYUIOPASDFGHJKLZXCVBNM'.split('').map(k=>button(k,'key')).join('')}${button('Space','key','space-key')}</div>`:''}</div>`;}
function dialog(){if(!state.dialog)return '';let title='',hint='',fields='',submit='connect';switch(state.dialog){
 case'manual':title=t('manual');hint=t('manualHint');fields=`<div class="field"><label for="draftIP">${t('targetIP')}</label><input id="draftIP" inputmode="decimal" value="${esc(state.draftIP)}" placeholder="192.168.1.24" autocomplete="off"></div><div id="manualIdentity" aria-live="polite">${manualIdentity()}</div>${passwordField(state.draft,'draft')}${rememberField()}`;break;
 case'edit':title=t('editTitle');hint=t('editHint');submit='save';fields=`<div class="field"><label for="draft">${t('newPassword')}</label><input id="draft" value="${esc(state.draft)}" autocomplete="off"></div><div class="draft-actions"><span class="small muted">${t('passwordHint')}</span>${button(t('generate'),'generate','generate-button','refresh')}</div><div class="callout">${icon('info')}<span>${t('editEffect')}</span></div>`;break;
 case'forget':title=t('forget');hint=t('forgetHint');submit='forgetConfirm';break;
 case'role':title=t('stopConfirm');hint=t('stopExplain');submit='stopConnect';break;
}return `<div class="overlay"><section class="dialog" role="dialog" aria-modal="true" aria-labelledby="dialogTitle"><h3 id="dialogTitle">${esc(title)}</h3><p>${esc(hint)}</p>${fields}${state.error?`<p class="inline-error" role="alert">${esc(state.error)}</p>`:''}<div class="buttons">${button(t('cancel'),'close')}${button(t(submit),'submit','primary')}</div></section></div>`;}
function render(){document.documentElement.lang=state.lang==='zh'?'zh-CN':'en';$('#reviewSubtitle').textContent=t('review');$('#reviewNote').textContent=t('sample');$('#platformLabel').textContent=t('platform');$('#languageLabel').textContent=t('language');$('#specLink').textContent=t('spec');$('#specLink').href=state.lang==='zh'?'../../docs/zh-CN/design.md':'../../docs/design.md';$('#platform').value=state.platform;$('#language').value=state.lang;$('#theme').innerHTML=icon('moon');$('#theme').setAttribute('aria-pressed',String(document.body.classList.contains('dark')));$('#reviewTabs').innerHTML=['connect','share','session','settings'].map(p=>`<button class="${state.page===p?'active':''}" data-page="${p}">${t(p)}</button>`).join('')+`<span class="caption">${t('preview')}${['windows','mac'].includes(state.platform)?'':' · future'}</span>`;
 const app=$('#app');app.className=`app ${mobile()?'mobile':''} ${state.fullscreen?'full-preview':''}`;app.dataset.platform=state.platform;app.innerHTML=chrome()+`<div class="app-body">${state.page==='session'?viewer():navigation()+`<div class="content">${state.page==='connect'?connect():state.page==='share'?share():settings()}</div>`}</div>`+(state.page==='session'?'':bottom())+dialog();
 const cases=state.page==='connect'?['normal','first','failed','manualScenario','empty','offlineScenario','role']:state.page==='share'?['normal','inUseScenario',...(state.platform==='mac'?['permission','view']:[])]:state.page==='session'?['normal','transferScenario','reconnectScenario']:['general','connection','clipboard','about'];
 $('#scenarios').innerHTML=`<span>${t('scenario')}</span>`+cases.map(k=>`<button class="${(state.page==='settings'?state.settingTab:state.scenario)===k?'active':''}" data-scenario="${k}">${t(k)}</button>`).join('');
}
let focusAction=null,pendingTarget=null;
function openDialog(type){focusAction=document.activeElement?.dataset.action;state.dialog=type;state.error='';state.inputReveal=false;if(type==='edit')state.draft='';render();$('.dialog input,.dialog button')?.focus();}
function closeDialog(){state.dialog=null;state.error='';state.draft='';state.draftIP='';render();if(focusAction)$(`[data-action="${focusAction}"]`)?.focus();}
function toast(message){$('.toast')?.remove();const el=document.createElement('div');el.className='toast';el.setAttribute('role','status');el.textContent=message;$('#app').append(el);setTimeout(()=>el.remove(),3500);}
function privateIP(value){if(!/^(\d{1,3}\.){3}\d{1,3}$/.test(value))return false;const a=value.split('.').map(Number);if(a.some(n=>n>255)||a[3]===0||a[3]===255)return false;return a[0]===10||a[0]===192&&a[1]===168||a[0]===172&&a[1]>=16&&a[1]<=31||a[0]===169&&a[1]===254;}
function startSession(){if(state.sharing){openDialog('role');return;}if(pendingTarget){const found=devices.findIndex(d=>d.ip===pendingTarget.ip);if(found<0){devices.push(pendingTarget);state.selected=devices.length-1;}else{state.selected=found;Object.assign(devices[found],pendingTarget);}pendingTarget=null;}
 const d=devices[state.selected];if(!d.saved)d.saved=state.remember;state.session={text:state.preferences.text,files:state.preferences.files,quality:state.preferences.quality,shortcuts:state.preferences.shortcuts};state.dialog=null;state.error='';state.draft='';state.draftIP='';state.page='session';state.scenario='normal';state.options=false;render();}
function validationError(message){state.error=message;const id=document.activeElement?.id;render();if(id)$(`#${id}`)?.focus();}
function action(name){switch(name){
 case'connect':if(!devices[state.selected].saved&&state.draft!=='Blue482X'){validationError(t('wrong'));return;}startSession();break;
 case'manual':state.draft='';state.draftIP='';openDialog('manual');break;
 case'edit':openDialog('edit');break;
 case'forget':openDialog('forget');break;
 case'close':pendingTarget=null;closeDialog();break;
 case'generate':state.draft=['Cobalt72','Violet83','Indigo64'].find(value=>value!==state.password&&value!==state.draft);$('#draft').value=state.draft;$('#draft').focus();break;
 case'submit':if(state.dialog==='manual'){const ip=state.draftIP.trim();if(!privateIP(ip)){validationError(t('ipError'));break;}if(state.draft!=='Blue482X'){validationError(t('wrong'));break;}const peer=peerFixtures.get(ip);pendingTarget={name:peer?.name||ip,os:peer?.os||'',kind:peer?.kind||'screen',ip,source:'authenticated',saved:state.remember,online:true};startSession();}
 else if(state.dialog==='edit'){const value=state.draft;if(!/^\S{8}$/u.test(value)){validationError(t('editValidate'));break;}if(value===state.password){validationError(t('unchanged'));break;}state.password=value;state.sharing=false;state.inUse=false;state.reveal=false;closeDialog();toast(t('savedToast'));}
 else if(state.dialog==='forget'){devices[state.selected].saved=false;closeDialog();}
 else if(state.dialog==='role'){state.sharing=false;state.inUse=false;state.dialog=null;startSession();}break;
 case'refresh':state.scenario='normal';state.error='';render();break;
 case'goShare':state.page='share';state.scenario='normal';state.error='';render();break;
 case'sharing':if(state.scenario==='permission'){toast(t('permHint'));break;}state.sharing=!state.sharing;state.inUse=false;render();break;
 case'reveal':state.reveal=!state.reveal;render();break;
 case'inputReveal':state.inputReveal=!state.inputReveal;render();(state.dialog?$('#draft'):$('#connectPassword'))?.focus();break;
 case'copy':toast(t('copied'));break;
 case'permissions':case'folder':case'key':toast(t('simulation'));break;
 case'diagnostics':state.diagnostics=!state.diagnostics;render();break;
 case'disconnect':state.page='connect';state.scenario='normal';state.options=false;state.keyboard=false;state.fullscreen=false;state.error='';render();break;
 case'cancelTransfer':state.transfer=false;render();toast(t('cancelled'));break;
 case'options':state.options=!state.options;render();break;
 case'release':state.options=false;render();toast(t('simulation'));break;
 case'fullscreen':state.fullscreen=!state.fullscreen;render();break;
 case'touch':state.touch=state.touch==='touchpad'?'direct':'touchpad';render();break;
 case'keyboard':state.keyboard=!state.keyboard;render();break;
}}
function selectScenario(key){state.dialog=null;state.error='';state.draft='';state.draftIP='';state.reveal=false;state.options=false;state.keyboard=false;pendingTarget=null;
 if(state.page==='settings'){state.settingTab=key;render();return;}state.scenario=key;
 if(state.page==='connect'){devices[0].saved=true;state.selected=key==='first'||key==='failed'?1:0;devices[1].saved=false;state.sharing=key==='role';state.inUse=false;if(key==='failed'){state.draft='WrongPassword';state.error=t('wrong');}if(key==='manualScenario'){openDialog('manual');return;}}
 if(state.page==='share'){state.sharing=key==='inUseScenario';state.inUse=key==='inUseScenario';}
 if(state.page==='session'){state.transfer=key==='transferScenario'&&state.preferences.files;state.session={text:state.preferences.text,files:state.preferences.files,shortcuts:state.preferences.shortcuts,quality:state.preferences.quality};}
 render();}
document.addEventListener('click',event=>{const b=event.target.closest('button');if(!b||b.disabled)return;
 if(b.dataset.page){state.page=b.dataset.page;state.scenario='normal';state.dialog=null;state.error='';state.draft='';state.options=false;state.fullscreen=false;pendingTarget=null;if(state.page==='session')state.session={...state.preferences};render();}
 else if(b.dataset.device!==undefined){state.selected=Number(b.dataset.device);state.draft='';state.error='';state.scenario='normal';render();}
 else if(b.dataset.settingTab){state.settingTab=b.dataset.settingTab;render();}
 else if(b.dataset.toggle){const scope=b.dataset.scope,key=b.dataset.toggle;state[scope][key]=!state[scope][key];if(scope==='preferences'&&!state.preferences[key]&&key in state.session)state.session[key]=false;if(key==='files'&&!state[scope].files)state.transfer=false;render();$(`[data-toggle="${key}"][data-scope="${scope}"]`)?.focus();}
 else if(b.dataset.scenario)selectScenario(b.dataset.scenario);
 else if(b.dataset.action)action(b.dataset.action);
});
document.addEventListener('input',event=>{const el=event.target;if(el.id==='connectPassword'||el.id==='draft')state.draft=el.value;if(el.id==='draftIP'){state.draftIP=el.value;updateManualIdentity();}});
document.addEventListener('change',event=>{const el=event.target;switch(el.id){
 case'platform':state.platform=el.value;state.dialog=null;state.error='';state.scenario='normal';state.draft='';state.reveal=false;state.sharing=false;state.inUse=false;state.options=false;state.keyboard=false;break;
 case'language':case'appLang':state.lang=el.value;break;
 case'remember':state.remember=el.checked;return;
 case'defaultQuality':state.preferences.quality=el.value;break;
 case'quality':state.session.quality=el.value;break;
 case'defaultShortcuts':state.preferences.shortcuts=el.value;break;
 case'sessionShortcuts':state.session.shortcuts=el.value;break;
 case'themePref':state.preferences.theme=el.value;document.body.classList.toggle('dark',el.value==='dark'||el.value==='system'&&matchMedia('(prefers-color-scheme:dark)').matches);break;
}render();$(`#${el.id}`)?.focus();});
document.addEventListener('keydown',event=>{
 if(state.dialog){if(event.key==='Escape'){event.preventDefault();pendingTarget=null;closeDialog();}else if(event.key==='Enter'&&event.target.tagName==='INPUT'){event.preventDefault();action('submit');}else if(event.key==='Tab'){const all=[...$('.dialog').querySelectorAll('button,input,select')].filter(el=>!el.disabled),first=all[0],last=all.at(-1);if(event.shiftKey&&document.activeElement===first){event.preventDefault();last.focus();}else if(!event.shiftKey&&document.activeElement===last){event.preventDefault();first.focus();}}}
 else if(event.key==='Escape'&&(state.options||state.keyboard||state.fullscreen)){state.options=false;state.keyboard=false;state.fullscreen=false;render();}
 else if(event.key==='Enter'&&event.target.id==='connectPassword'){event.preventDefault();action('connect');}
});
$('#theme').addEventListener('click',()=>{document.body.classList.toggle('dark');$('#theme').setAttribute('aria-pressed',String(document.body.classList.contains('dark')));});
render();
