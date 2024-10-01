export function initialize(drop_area) {
    drop_area.addEventListener('dragover', (e) => {
        e.preventDefault();
    });

    drop_area.addEventListener('drop', (e) => {
        e.preventDefault();

        var hiddenInputFile = document.querySelector('.hidden-input-file');

        hiddenInputFile.files = e.dataTransfer.files;

        var customChangeEvent = new Event('change', { bubbles: true });

        hiddenInputFile.dispatchEvent(customChangeEvent);
    });
}