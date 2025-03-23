let titles = [
    "Access your <span>services</span> at <span>anytime</span>",
    "Access your <span>website</span> from <span>anywhere</span>", 
    "Access your <span>website</span> at <span>anytime</span>", 
    "Access your <span>minecraft server</span> from <span>anywhere<span>", 
    "Access your <span>minecraft server</span> at <span>anytime<span>", 
    "Access your <span>services</span> from <span>anywhere</span>"];
let index = 0;
let title = document.getElementById("h2changing");

function changeTitle() {
    title.classList.remove("animate");
    setTimeout(() => {
      title.innerHTML = titles[index];
      title.classList.add("animate");
      index = (index + 1) % titles.length;
    }, 500);
}

setInterval(changeTitle, 2000);
