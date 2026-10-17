// Static visual-study exports, not browser/native screenshots.
// NODE_PATH must contain sharp. Run from any directory with Node.js.
const fs=require('fs'),path=require('path'),sharp=require('sharp');
const dir=__dirname;
const esc=s=>String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const text=(x,y,s,size=14,color='#252d40',anchor='start',weight=400)=>`<text x="${x}" y="${y}" font-size="${size}" fill="${color}" text-anchor="${anchor}" font-weight="${weight}">${esc(s)}</text>`;
const rect=(x,y,w,h,r,fill,stroke='none')=>`<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${r}" fill="${fill}" stroke="${stroke}"/>`;
const line=(x,y,x2,y2,color='#e3e7ef')=>`<path d="M${x} ${y}H${x2}" stroke="${color}"/>`;
const asset=(name,x,y,w,h)=>`<image x="${x}" y="${y}" width="${w}" height="${h}" href="data:image/svg+xml;base64,${fs.readFileSync(path.join(dir,name)).toString('base64')}"/>`;
const button=(x,y,w,h,label,primary=false)=>rect(x,y,w,h,11,primary?'#5265ec':'#fff',primary?'#5265ec':'#dde4f0')+text(x+w/2,y+h/2+5,label,13,primary?'#fff':'#5265ec','middle');
const dots=(x,y,good=false)=>`<circle cx="${x}" cy="${y-4}" r="3" fill="${good?'#28745e':'#748297'}"/>`;
function chrome(){return rect(0,0,600,680,14,'#fff','#dce2ec')+asset('../../assets/brand/icon.svg',13,10,15,15)+text(37,22,'Local Remote Desktop',11,'#637084')+text(486,22,'−',14,'#637084')+text(530,21,'□',11,'#637084')+text(573,22,'×',15,'#637084');}
function top(home=false){return (home?'':text(32,70,'‹ 返回',12,'#637084'))+text(568,70,'⚙ 设置',12,'#637084','end');}
function heading(name,hint){return top()+text(32,119,name,26)+text(32,145,hint,12,'#637084');}
function home(){return chrome()+top(true)+text(300,148,'你想如何使用？',27,'#252d40','middle')+asset('connection-stage.svg',50,164,500,266)+rect(32,464,536,80,15,'#fff','#dce3f0')+`<path d="M300 464V544" stroke="#dce3f0"/>`+text(166,492,'↗',20,'#5265ec','middle')+text(166,523,'发起连接',17,'#252d40','middle')+text(434,492,'↙',20,'#5265ec','middle')+text(434,523,'接受连接',17,'#252d40','middle');}
function connect(){let out=chrome()+heading('发起连接','选择你要连接并操作的设备。')+text(32,190,'设备',12,'#637084')+text(437,190,'↻',18,'#637084')+button(468,167,100,34,'＋ 添加目标');
out+=rect(32,215,536,188,15,'#f4f6ff','#e4e9f2')+asset('device-display.svg',54,249,184,123)+text(269,289,'Studio Mac',20)+text(269,316,'macOS · 已记住',12,'#637084')+dots(273,346,true)+text(283,346,'可连接',11,'#28745e');
out+=rect(32,416,262,179,15,'#f8faff','#e4e9f2')+asset('device-display.svg',47,442,116,77)+text(48,548,'Office PC',14)+text(48,571,'Windows · 首次连接',11,'#637084')+dots(241,439,true)+text(250,439,'可连接',10,'#28745e');
out+=rect(306,416,262,179,15,'#fafbfc','#e4e9f2')+`<g opacity=".4">${asset('device-display.svg',321,442,116,77)}</g>`+text(322,548,'Travel Mac',14,'#748297')+text(322,571,'macOS · 已记住',11,'#748297')+dots(524,439)+text(533,439,'离线',10,'#748297');return out;}
function accept(){let out=chrome()+heading('接受连接','让另一台设备连接并操作你的当前屏幕。')+rect(239,168,122,25,13,'#f2faf6','#cee5da')+dots(253,185,true)+text(265,185,'等待连接',11,'#28745e')+asset('device-display.svg',218,201,164,109)+text(300,325,'本机名称',10,'#637084','middle')+text(300,351,'My MacBook',19,'#252d40','middle')+text(390,351,'修改',11,'#5265ec');
out+=rect(32,373,536,160,14,'#fff','#e2e8f1')+text(49,395,'本机 IP',11,'#637084')+text(49,419,'192.168.1.12',15,'#394561')+text(550,412,'复制',11,'#5265ec','end')+line(49,435,551,435)+text(49,459,'当前密码',12)+text(550,459,'修改密码',11,'#5265ec','end')+text(49,490,'• • • • • • • •',22,'#48567a')+text(509,487,'查看',11,'#5265ec','end')+text(550,487,'复制',11,'#5265ec','end')+text(49,514,'连接本设备时，使用下方密码。',11,'#718098');
out+=text(34,558,'✓',14,'#28745e')+text(58,557,'屏幕录制',11)+text(565,557,'已允许',10,'#28745e','end')+text(34,584,'✓',14,'#28745e')+text(58,583,'键盘与鼠标控制',11)+text(565,583,'已允许',10,'#28745e','end')+rect(32,613,536,45,12,'#fff','#dec1c8')+text(300,641,'停止接受连接',13,'#ad3544','middle');return out;}
const wrap=(w,h,body)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><style>text{font-family:'Microsoft YaHei UI','Microsoft YaHei','Segoe UI',sans-serif}</style>${body}</svg>`;
async function main(){const views={home:home(),connect:connect(),accept:accept()};for(const [name,body]of Object.entries(views)){const svg=wrap(600,680,body);fs.writeFileSync(path.join(dir,`desk-${name}.svg`),svg);await sharp(Buffer.from(svg)).resize(900,1020).png().toFile(path.join(dir,`desk-${name}.png`));}
let board=rect(0,0,1960,906,0,'#eef1f6')+text(40,49,'Local Remote Desktop',25)+text(40,80,'屏幕连接台 · 整体设计提案',15,'#637084');
for(const [i,[name,label]]of Object.entries(Object.entries({home:'首页 · 一个场景，两个角色',connect:'发起连接 · 设备展陈',accept:'接受连接 · 围绕本机组织信息'}))){const x=40+Number(i)*640;board+=text(x,119,label,15)+`<g transform="translate(${x} 141)">${views[name]}</g>`;}
board+=text(40,864,'静态设计稿，非客户端或浏览器截图。设备、密码与状态均为虚构示例；交互请打开 desk.html。',12,'#637084');
await sharp(Buffer.from(wrap(1960,906,board))).png().toFile(path.join(dir,'desk-overview.png'));
const old=await sharp(path.join(dir,'../entry-review/windows-home-zh.png')).resize({width:540,height:612,fit:'contain',background:'#fff'}).png().toBuffer();
let compare=rect(0,0,1320,876,0,'#eef1f6')+text(40,48,'这次改变的是界面的组织方式',25)+text(40,78,'保留已确认的角色与品牌；重新设计首页、设备选择、接受连接和设置。',14,'#637084')+text(40,121,'之前：两张入口卡片，插图各自独立',15,'#637084')+text(680,121,'新版：完整屏幕场景，操作入口融为一体',15,'#252d40');
compare+=rect(40,141,600,680,14,'#fff','#dce2ec')+`<image x="70" y="175" width="540" height="612" href="data:image/png;base64,${old.toString('base64')}"/>`+`<g transform="translate(680 141)">${views.home}</g>`+text(40,852,'左：历史网页预览；右：新的静态设计导出。不是原生界面实测。',12,'#637084');
await sharp(Buffer.from(wrap(1320,876,compare))).png().toFile(path.join(dir,'comparison.png'));
console.log('Exported three static screens, overview and comparison. Not browser screenshots.');}
main().catch(e=>{console.error(e.message);process.exitCode=1});
