const doc = document.getElementById("doc")

async function FetchMarkdown() {
    const response = await fetch("https://fluentport.com/documents/terms-of-service.md")

    const text = await response.text()
    doc.innerHTML = marked.parse(text)
}

FetchMarkdown();
