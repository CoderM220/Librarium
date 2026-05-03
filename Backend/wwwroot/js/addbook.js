const isbnInput = document.getElementById('isbnInput');
const coverPreview = document.getElementById('coverPreview');

if (isbnInput) {
    isbnInput.addEventListener('blur', async () => {
        const isbn = isbnInput.value.trim().replace(/-/g, '');
        if (isbn.length < 10) return;

        const coverUrl = `https://covers.openlibrary.org/b/isbn/${isbn}-L.jpg`;

        // Check if cover exists by loading it
        const img = new Image();
        img.onload = () => {
            if (img.width > 1) {
                document.getElementById('CoverUrl').value = coverUrl;
                if (coverPreview) {
                    coverPreview.src = coverUrl;
                    coverPreview.style.display = 'block';
                }
            }
        };
        img.src = coverUrl;

        // Also fetch metadata
        try {
            const res = await fetch(`https://openlibrary.org/api/books?bibkeys=ISBN:${isbn}&format=json&jscmd=data`);
            const data = await res.json();
            const key = `ISBN:${isbn}`;
            if (data[key]) {
                const book = data[key];
                if (book.title) document.querySelector('input[name="Title"]').value = book.title;
                if (book.authors?.[0]?.name) document.querySelector('input[name="Author"]').value = book.authors[0].name;
                if (book.publish_date) {
                    const year = parseInt(book.publish_date.match(/\d{4}/)?.[0]);
                    if (year) document.querySelector('input[name="Year"]').value = year;
                }
            }
        } catch (e) { console.log('Open Library fetch failed', e); }
    });
}