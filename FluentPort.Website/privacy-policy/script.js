const doc = document.getElementById("doc")

async function FetchMarkdown() {
    const response = await fetch("https://fluentport.com/documents/privacy-policy.md")

    const text = await response.text()
    doc.innerHTML = marked.parse(text)
}

FetchMarkdown();
