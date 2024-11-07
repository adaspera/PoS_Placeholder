import {Button, Col, Row} from "reactstrap";
import { useEffect, useState } from "react";

const Home = () => {
    const [totalPrice, setTotalPrice] = useState("0");
    const [order, setOrder] = useState({
        products: [
            { id: 1, fullName: "Item 1", price: 10.99, quantity: 2 },
            { id: 2, fullName: "Item 2", price: 15.49, quantity: 1 },
            { id: 3, fullName: "Item 3", price: 7.99, quantity: 3 },
            { id: 4, fullName: "Item 4", price: 12.99, quantity: 1 },
            { id: 5, fullName: "Item 5", price: 4.99, quantity: 5 }
        ]
    });

    const [products, setProducts] = useState(null);

    const removeItem = (id) => {
        const updatedProducts = order.products.filter((item) => item.id !== id);

        setOrder((prevOrder) => ({ ...prevOrder, products: updatedProducts }));
    };

    useEffect(() => {
        formatProducts();

        const total = order.products.reduce((acc, item) => acc + item.price * item.quantity, 0);
        setTotalPrice(total.toFixed(2));
    }, [order.products]);

    const formatProducts = () => {
        const formatedProducts = order.products.map((item, index) => (
            <Row key={index} className="p-2">
                <Col>{item.fullName}</Col>
                <Col className="d-flex justify-content-center">x{item.quantity}</Col>
                <Col className="d-flex justify-content-end">
                    {item.price} €
                    <i
                        className="bi-x-circle px-2"
                        style={{ cursor: "pointer" }}
                        onClick={() => removeItem(item.id)}
                    ></i>
                </Col>
            </Row>
        ));
        setProducts(formatedProducts); // Update the products state with the formatted list
    };

    return (
        <Row style={{ height: "85vh" }}>
            <Col className="border rounded shadow-sm m-2 p-1 col-lg-3 d-flex flex-column">
                <div className="justify-content-center border rounded p-2">
                    Total: {totalPrice} €
                </div>
                <div>
                    {products}
                </div>
                <div className="d-flex justify-content-center m-2 mt-auto">
                    <Button color="dark" outline className="m-1">Split order</Button>
                    <Button color="dark" outline className="m-1">Pay later</Button>
                    <Button color="success" className="m-1">Pay now</Button>
                </div>
            </Col>
            <Col className="border rounded shadow-sm m-2 p-2">miau</Col>
        </Row>
    );
};

export default Home;
