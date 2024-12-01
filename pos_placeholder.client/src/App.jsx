import { Route, Routes } from 'react-router-dom';
import AppRoutes from "@/AppRoutes.jsx";
import Layout from './Layout';
import {login} from "@/api/AuthService.jsx";

function App() {
    //temp bypass
    login("owner@gmail.com","Owner123*")
    return (
        <Layout>
            <Routes>
                {AppRoutes.map((route, index) => {
                    const { element, ...rest } = route;
                    return <Route key={index} {...rest} element={element} />;
                })}
            </Routes>
        </Layout>
    );
}

export default App;