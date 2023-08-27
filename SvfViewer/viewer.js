var myViewerDiv = document.getElementById('preview');
var mypdfDiv = document.getElementById('pdfview');
var viewer1 = new Autodesk.Viewing.GuiViewer3D(myViewerDiv);
var viewer2 = new Autodesk.Viewing.GuiViewer3D(mypdfDiv);
var options = {
    'env': 'Local'

};

var views = [
    {
        document: './output/dwg.svf',
        sharedPropertyDbPath: `${window.location.origin}/output/`
    },
    {
        document: './output/Model.pdf',
        sharedPropertyDbPath: `${window.location.origin}/output/`
    }
]

Autodesk.Viewing.Initializer(options, function () {
    viewer1.start(views[0].document, views[0]);
    viewer2.start(views[1].document, views[1]);
});