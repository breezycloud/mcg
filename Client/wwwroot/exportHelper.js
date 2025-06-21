function downloadReport(reportName, byteArray) {
    var link = document.createElement('a');
    link.download = reportName;
    link.href = "data:application/octet-stream;base64," + byteArray;
    document.body.appendChild(link); // Needed for Firefox
    link.click();
    document.body.removeChild(link);
}       

function jsCallback(dotnetReference) {
    dotnetReference.invokeMethodAsync("ProcessResult", employeeJson);
}
async function exportFile(fileName, contentStreamReference) {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
    URL.revokeObjectURL(url);
}

async function importFile(contentStreamReference) {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);    
    const contents = new Array();
    if (blob) {     
        try {
            var fileReader = new FileReader();
            fileReader.readAsBinaryString(blob);
            fileReader.onload = function (event) {
                var data = event.target.result;

                var workbook = XLSX.read(data, {
                    type: "binary",
                    cellDates: true
                });

                workbook.SheetNames.forEach((sheet) => {
                    let rowObject = XLSX.utils.sheet_to_row_object_array(
                        workbook.Sheets[sheet]
                    );
                    var json = JSON.stringify(rowObject);                    
                    contents.push(json)                    
                });                
                if (contents.length > 0) {
                    localStorage.setItem("file", contents);
                }
            };             
        } catch (e) {
            console.log(e);
        }        
    }        
    
}

async function shareInvite() {
    try {
        await navigator.share({
            files: files,
            title: 'UZHII',
            text: 'Order Receipt'
        });
        return true;
    } catch (err) {
        console.log(`Error: ${err}`);
    }
    files = [];
    return false;
}
let files = [];
function getFile(fileName, mime, format, blob) {
    var file = new File([blob], fileName, { type: `${mime}/${format}` })
    files.push(file);
    console.log(files);
}
function clearFiles() {
    files = [];
    console.log(files);
}