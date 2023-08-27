const express = require('express');
const app = express();
const path = require('path');

// Serve files from a specific directory
const filesDirectory = path.join(__dirname, 'output');
app.use(express.static(filesDirectory));

// Start the server
const port = 8080; // Choose a port number
app.listen(port, () => {
    console.log(`Server started on http://localhost:${port}`);
});