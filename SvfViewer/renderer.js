/**
 * This file is loaded via the <script> tag in the index.html file and will
 * be executed in the renderer process for that window. No Node.js APIs are
 * available in this process because `nodeIntegration` is turned off and
 * `contextIsolation` is turned on. Use the contextBridge API in `preload.js`
 * to expose Node.js functionality from the main process.
 */
const btn = document.getElementById('btn')
var script = document.createElement('script');
var x = document.createElement("IMG")
x.setAttribute("class", "center")
x.setAttribute("src", "assets/poweredbyAutodesk.svg");
x.setAttribute("width", "200");
x.setAttribute("height", "100");
document.body.appendChild(x);

document.getElementsByTagName('head')[0].appendChild(script);
script.type = 'text/javascript';
btn.addEventListener('click', async () => {
    const filePath = await window.electronAPI.openFile()
    if (filePath) {
        document.body.removeChild(x);
        script.src = 'viewer.js'
    }
})
