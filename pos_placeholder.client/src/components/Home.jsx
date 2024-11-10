import {Button, Col, Modal, ModalBody, ModalFooter, Row} from "reactstrap";
import { useEffect, useState } from "react";

const Home = () => {
    const [totalPrice, setTotalPrice] = useState("0");
    const [order, setOrder] = useState(null);
    const [productsInCart, setProductsInCart] = useState(null);
    const [productsInCatalogue, setProductsInCatalogue] = useState(null);

    const [selectedProduct, setSelectedProduct] = useState(null);
    const [variations, setVariations] = useState([]);


    const getProductVariations = (productId) => {
        const variationsDTO = [
            {
                id: 0,
                name: "Variation A",
                price: 10,
                inventoryQuantity: 20,
                productId: productId,
                picture: "imageA.png",
                discountId: null
            },
            {
                id: 1,
                name: "Variation B",
                price: 12,
                inventoryQuantity: 15,
                productId: productId,
                picture: "imageB.png",
                discountId: 1
            }
        ];

        setVariations(variationsDTO);
    };

    const getOrder = (id) => {
        //const OrderDTO = fetch("https://",{method: "GET"})
        //    .then((response) => response.json())
        const OrderDTO = order;

        return OrderDTO;
    }

    const createOrder = () => {
        //const OrderDTO = fetch("https://",{method: "GET"})
        //    .then((response) => response.json())
        const OrderDTO = {
            products: [
                { id: 1, fullName: "Item 1", price: 10.99, quantity: 2 },
                { id: 2, fullName: "Item 2", price: 15.49, quantity: 1 },
                { id: 3, fullName: "Item 3", price: 7.99, quantity: 3 },
                { id: 4, fullName: "Item 4", price: 12.99, quantity: 1 },
                { id: 5, fullName: "Item 5", price: 4.99, quantity: 5 }
            ]
        };
        return OrderDTO;
    }

    const getProducts = () => {
        // const ProductsDTO = fetch();
        const productsDTO = [
            { id: 1, name: "Item 1", itemGroup: "Group1" },
            { id: 2, name: "Item 2", itemGroup: "Group1" },
            { id: 3, name: "Item 3", itemGroup: "Group2" },
            { id: 4, name: "Item 4", itemGroup: "Group2" },
            { id: 5, name: "Item 5", itemGroup: "Group2" }
        ];

        return productsDTO;
    }


    const removeItemFromCart = (id) => {
        const updatedProductsInCart = order.products.filter((item) => item.id !== id);

        setOrder((prevOrder) => ({ ...prevOrder, products: updatedProductsInCart }));
    };

    const handleProductClick = (product) => {
        setSelectedProduct(product);
        getProductVariations(product.id);
    };

    useEffect( () => {
        if (!order) {
            setOrder(createOrder());
        }

        formatProductsInCatalogue(getProducts());
    }, [])

    useEffect(() => {
        if (order && order.products) {
            formatProductsInCart();

            const total = order.products.reduce((acc, item) => acc + item.price * item.quantity, 0);
            setTotalPrice(total.toFixed(2));
        }
    }, [order]);

    const formatProductsInCart = () => {
        const formatedProductsInCart = order.products.map((item, index) => (
            <Row key={index} className="p-2">
                <Col>{item.fullName}</Col>
                <Col className="d-flex justify-content-center">x{item.quantity}</Col>
                <Col className="d-flex justify-content-end">
                    {item.price} €
                    <i
                        className="bi-x-circle px-2"
                        style={{ cursor: "pointer" }}
                        onClick={() => removeItemFromCart(item.id)}
                    ></i>
                </Col>
            </Row>
        ));

        setProductsInCart(formatedProductsInCart);
    };

    const formatProductsInCatalogue = (products) => {
        const itemsPerRow = 6;
        const groupedProducts = products.reduce((acc, product) => {
            acc[product.itemGroup] = acc[product.itemGroup] || [];
            acc[product.itemGroup].push(product);
            return acc;
        }, {});

        const catalogue = Object.entries(groupedProducts).map(([groupName, groupProducts]) => (
            <div key={groupName}>
                <div className="mb-3">{groupName}</div>
                {Array.from({ length: Math.ceil(groupProducts.length / itemsPerRow) }, (_, rowIndex) => {
                    const rowItems = groupProducts.slice(rowIndex * itemsPerRow, (rowIndex + 1) * itemsPerRow);

                    return (
                        <Row key={rowIndex} className="pb-4">
                            {rowItems.map((item) => (
                                <Col key={item.id} md={12 / itemsPerRow} xs={6}>
                                    <div className="border rounded p-2" style={{cursor: "pointer"}} onClick={() => handleProductClick(item) }>
                                        {item.name}
                                    </div>
                                </Col>
                            ))}
                        </Row>
                    );
                })}
            </div>
        ));

        setProductsInCatalogue(catalogue);
    }

    const modal =
        <Modal isOpen={!!selectedProduct} fade={false} size="lg" centered={true}>
            <ModalBody>
                <h5>{selectedProduct?.name}</h5>
                {variations.map((variation) => (
                    <div key={variation.id} className="p-2 border rounded mb-2">
                        <h6>{variation.name}</h6>
                        <p>Price: {variation.price} €</p>
                        <p>In Stock: {variation.inventoryQuantity}</p>
                        {variation.picture && <img src={variation.picture} alt={variation.name} width="50" />}
                    </div>
                ))}
            </ModalBody>
            <ModalFooter>
                <Button color="secondary" onClick={() => setSelectedProduct(null)}>
                    Cancel
                </Button>
            </ModalFooter>
        </Modal>

    return (
        <Row style={{ height: "85vh" }}>
            <Col className="border rounded shadow-sm m-2 p-1 d-flex flex-column" lg={4}>
                <div className="justify-content-center border rounded p-2">
                    Total: {totalPrice} €
                </div>
                <div>
                    {productsInCart}
                </div>
                <div className="d-flex justify-content-center m-2 mt-auto">
                    <Button color="dark" outline className="m-1">Split order</Button>
                    <Button color="dark" outline className="m-1">Pay later</Button>
                    <Button color="success" className="m-1">Pay now</Button>
                </div>
            </Col>
            <Col className="border rounded shadow-sm m-2 p-2">{productsInCatalogue}</Col>

            {modal}
        </Row>
    );
};

export default Home;
