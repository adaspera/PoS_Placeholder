import Home from "./components/Home.jsx";
import Business from "@/components/Business.jsx";
import Products from "@/components/Products.jsx";
import Orders from "@/components/Orders.jsx";
import Tax from "@/components/Tax.jsx";


const AppRoutes = [
    {
        index: true,
        element: <Home />
    },
    {
        path: '/business',
        element: <Business />
    },
    {
        path: '/tax',
        element: <Tax />
    },
    {
        path: '/products',
        element: <Products />
    },
    {
        path: '/orders',
        element: <Orders />
    }

];

export default AppRoutes;