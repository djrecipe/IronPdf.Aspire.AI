export function initialize(drop_area, drop_area2) {
    var hiddenInputFile = document.querySelector('.hidden-input-file');
    var hiddenInputFile2 = document.querySelector('.hidden-input-file2');

    drop_area.addEventListener('dragover', (e) => {
        e.preventDefault();
    });

    drop_area.addEventListener('drop', (e) => {
        e.preventDefault();

        hiddenInputFile.files = e.dataTransfer.files;

        var customChangeEvent = new Event('change', { bubbles: true });

        hiddenInputFile.dispatchEvent(customChangeEvent);
    });

    drop_area2.addEventListener('dragover', (e) => {
        e.preventDefault();
    });

    drop_area2.addEventListener('drop', (e) => {
        e.preventDefault();

        hiddenInputFile2.files = e.dataTransfer.files;

        var customChangeEvent = new Event('change', { bubbles: true });

        hiddenInputFile2.dispatchEvent(customChangeEvent);
    });
}

export function browse_file(hidden_input_file) {
    hidden_input_file.click();
}