// Helper function to download files from base64 data
window.downloadFile = function (fileName, base64Data) {
    const linkSource = `data:application/octet-stream;base64,${base64Data}`;
    const downloadLink = document.createElement("a");
    downloadLink.href = linkSource;
    downloadLink.download = fileName;
    downloadLink.click();
};
