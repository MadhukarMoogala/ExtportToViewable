// Modules to control application life and create native browser window
const { app, BrowserWindow, ipcMain, dialog } = require('electron')
const path = require('path')
const AdmZip = require('adm-zip');

async function handleFileOpen() {
  const { canceled, filePaths } = await dialog.showOpenDialog()
  if (!canceled) {
    const zipFile = filePaths[0];
    const destDir = "output";
    // Create an instance of AdmZip
    const zip = new AdmZip(zipFile);
    zip.extractAllTo(destDir, /*overwrite*/ true);
    return zipFile;
  }
}
// Keep a global reference of the window object, if you don't, the window will
// be closed automatically when the JavaScript object is garbage collected.
let mainWindow;

function createWindow() {
  // Create the browser window.
  mainWindow = new BrowserWindow({
    width: 800,
    height: 600,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      plugins: true,
      nodeIntegration: true,
      enableRemoteModule: true
    },
    frame: true,
    titleBarOverlay: {
      color: 'white',
      symbolColor: '#000000',
      height: '3em'

    },
    titleBarStyle: 'hidden',
    icon: 'assets/viewer-white.png'
  })
  // and load the index.html of the app.
  mainWindow.loadFile('index.html');
  // Emitted when the window is closed.
  mainWindow.on('closed', () => {
    // Dereference the window object, usually you would store windows
    // in an array if your app supports multi windows, this is the time
    // when you should delete the corresponding element.
    mainWindow = null;

  });


  // Open the DevTools.
  // mainWindow.webContents.openDevTools()

}
// This method will be called when Electron has finished
// initialization and is ready to create browser windows.
// Some APIs can only be used after this event occurs.
app.whenReady().then(() => {
  ipcMain.handle('dialog:openFile', handleFileOpen)
  createWindow()
})

app.on('activate', () => {
  // On macOS it's common to re-create a window in the app when the
  // dock icon is clicked and there are no other windows open.
  if (BrowserWindow.getAllWindows().length === 0) { createWindow(); }
})

// Quit when all windows are closed, except on macOS. There, it's common
// for applications and their menu bar to stay active until the user quits
// explicitly with Cmd + Q.
app.on('window-all-closed', function () {
  if (process.platform !== 'darwin') app.quit()
})


