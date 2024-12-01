import {Button, Col, Modal, ModalBody, ModalFooter, Row} from "reactstrap";
import { useEffect, useState } from "react";
import {getProducts, getProductVariations} from "@/api/productApi.jsx";
import {createOrder} from "@/api/orderApi.jsx";

const Home = () => {
    const [isLoading, setIsLoading] = useState(true);

    const [totalPrice, setTotalPrice] = useState("0");
    const [order, setOrder] = useState(null);
    const [products, setProducts] = useState(getProducts());
    const [productsInCart, setProductsInCart] = useState(null);
    const [productsInCatalogue, setProductsInCatalogue] = useState(null);

    const [selectedProduct, setSelectedProduct] = useState(null);
    const [variations, setVariations] = useState([]);


    const removeItemFromCart = (id) => {
        const updatedProductsInCart = order.products.filter((item) => item.id !== id);

        setOrder((prevOrder) => ({ ...prevOrder, products: updatedProductsInCart }));
    };

    const handleProductClick = (product) => {
        setSelectedProduct(product);
        setVariations(getProductVariations(product.id));
    };

    const handleAddToCart = (variation) => {
        // does nothing for now
        setProductsInCart((prevProducts) => ({ ...prevProducts}));
    };


    const fetchProducts = async () => {
        try {
            const fetchedProducts = await getProducts();
            setProducts(fetchedProducts);
        } catch (error) {
            console.error("Error fetching products:", error);
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        setOrder(createOrder());
    }, []);

    useEffect(() => {
        fetchProducts();
    }, []);

    useEffect(() => {
        if (order && order.products) {
            formatProductsInCart();

            const total = order.products.reduce((acc, item) => acc + item.price * item.quantity, 0);
            setTotalPrice(total.toFixed(2));
        }
    }, [order]);

    useEffect(() => {
        if (products && products.length > 0) {
            formatProductsInCatalogue(products);
        }
    }, [products]);

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
                    <div key={variation.id} className="p-2 border rounded mb-2" onClick={() => handleAddToCart(variation)}>
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

    if (isLoading) {
        return <div>Loading...</div>;
    }
    console.log(products);

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
            <Col className="border rounded shadow-sm m-2 p-2">
                {productsInCatalogue}
            </Col>

            {modal}
        </Row>
    );
};

export default Home;
