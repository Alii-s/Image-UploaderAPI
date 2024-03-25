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

form.addEventListener("submit", async (e) => {
    e.preventDefault();
    if (validate()) {
        const formData = new FormData();
        formData.append("img", imgInput.files[0]);
        formData.append("name", nameInput.value);

        console.log("Form Data:", formData); // Log for debugging

        try {
            const response = await fetch("/api", {
                method: "POST",
                body: formData
            });

            console.log("Response Status:", response.status); // Log response status for debugging

            if (response.ok) {
                const responseData = await response.text();
                console.log("Response Data:", responseData); // Log response data for debugging
                window.location.href = `/picture/${responseData}`;
            } else {
                const errorData = await response.text()
                console.log("Error:", errorData);
            }
        } catch (error) {
            console.error("Error:", error);
        }
    }
});
