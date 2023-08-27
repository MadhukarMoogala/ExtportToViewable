@echo off
set curdir="%cd%"
pushd D:\Temp\SvfViewer
call npm start
echo "Viewer Close"
popd

