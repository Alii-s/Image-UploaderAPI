const imgInput = document.querySelector("#imgExtension");
const nameInput = document.querySelector("#imageName");
const form = document.querySelector("form");
function validate() {
    if (nameInput.value === "") {
        document.querySelector(".nameValidation").classList.remove("d-none");
        return false;
    } else {
        document.querySelector(".nameValidation").classList.add("d-none");
    }
    if (imgInput.value === "" || !imgInput.value.match(/\.(jpeg|png)$/)) {
        document.querySelector(".imgValidation").classList.remove("d-none");
        return false;
    } else {
        document.querySelector(".imgValidation").classList.add("d-none");
    }
    return true;
}

imgInput.addEventListener("change", validate);
nameInput.addEventListener("change", validate);

form.addEventListener("submit", (e) => {
    e.preventDefault();
    if (validate()) {
        const formData = new FormData();
        formData.append("img", imgInput.files[0]);
        formData.append("name", nameInput.value);
        axios.post("/api", formData).then((res) => {
            if(res.status === 200) {
                window.location.href = `/picture/${res.data}`;
            }
        })
        .catch((err) => {
            console.log("Error: ", err.response.data);
        });
    }
});
