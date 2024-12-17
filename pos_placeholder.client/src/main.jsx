import {BrowserRouter} from 'react-router-dom'
import {createRoot} from 'react-dom/client'
import 'bootstrap/dist/css/bootstrap.css'
import 'bootstrap-icons/font/bootstrap-icons.css'
import App from './App.jsx'
import {ToastContainer, toast} from 'react-toastify';
import 'react-toastify/dist/ReactToastify.css';


createRoot(document.getElementById('root')).render(
    <BrowserRouter>
        <ToastContainer/>
        <App/>
    </BrowserRouter>
)
