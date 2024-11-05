import './App.css';
import { Route, Routes } from 'react-router-dom';
import AppRoutes from "@/AppRoutes.jsx";
import Layout from './Layout';

function App() {
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