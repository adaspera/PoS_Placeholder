import {toast, Bounce} from "react-toastify";

const toastNotify = (message, toastType) => {
    toast(message, {
        type: toastType,
        position: "top-center",
        autoClose: 5000,
        hideProgressBar: false,
        closeOnClick: true,
        pauseOnHover: true,
        draggable: true,
        progress: undefined,
        theme: "colored",
        transition: Bounce,
    });
}

export default toastNotify;