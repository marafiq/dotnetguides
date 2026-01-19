const http = require('http');
const fs = require('fs');
const path = require('path');

const PORT = 3001;

const server = http.createServer((req, res) => {
    const filePath = path.join(__dirname, 'index.html');
    fs.readFile(filePath, (err, content) => {
        if (err) {
            res.writeHead(500);
            res.end('Error loading index.html');
            return;
        }
        res.writeHead(200, { 'Content-Type': 'text/html' });
        res.end(content);
    });
});

server.listen(PORT, '0.0.0.0', () => {
    console.log(`
╔════════════════════════════════════════════════════════════════╗
║     SHALIMAR REACT APP - MyProfile Component Demo              ║
╚════════════════════════════════════════════════════════════════╝

React app running at: http://localhost:${PORT}

This is a REAL React application running the MyProfile component
that was generated from MyProfile.razor using Shalimar.Razor compiler.

Pipeline: MyProfile.razor → Shalimar Compiler → MyProfile.tsx → React App
`);
});
