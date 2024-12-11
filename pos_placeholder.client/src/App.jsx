import { Route, Routes } from 'react-router-dom';
import AppRoutes from "@/AppRoutes.jsx";
import Layout from './Layout';
import Login from "@/components/shared/Login.jsx";
import {useState} from "react";

function App() {
    const [isAuthenticated, setIsAuthenticated] = useState(!!localStorage.getItem("authToken"));

    const handleLogout = () => {
        setIsAuthenticated(false);
    };

    return (
        <>
            {isAuthenticated ? (
                <Layout onLogout={() => handleLogout()}>
                    <Routes>
                        {AppRoutes.map((route, index) => {
                            const { element, ...rest } = route;
                            return <Route key={index} {...rest} element={element} />;
                        })}
                    </Routes>
                </Layout>
            ) : (
                <Login onLogin={() => setIsAuthenticated(true)} />
            )}
        </>
    );
}

export default App;