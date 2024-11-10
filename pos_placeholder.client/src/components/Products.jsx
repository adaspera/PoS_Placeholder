import {useEffect, useState} from "react";
import {getProducts, getProductVariations} from "@/api/productApi.jsx";
import {Accordion, AccordionBody, AccordionHeader, AccordionItem, Button, Col, Input, Label, Row} from "reactstrap";

const Products = () => {

    const [products, setProducts] = useState([]);
    const [variations, setVariations] = useState([]);

    useEffect( () => {
        setProducts(getProducts());
    },[]);


    const [open, setOpen] = useState(null);

    const toggleAccordion = (id) => {
        if (open === id) {
            setOpen(null);
            setVariations([])
        } else {
            setOpen(id);
            setVariations(getProductVariations(id));
        }
    };

    const removeProduct = (id) => {
        setProducts(products.filter(product => product.id !== id));
    }

    const removeVariant = (id) => {
        setVariations(variations.filter(variation => variation.id !== id));
    }

    return (
        <Row style={{ height: "85vh" }}>
            <Col className="border rounded shadow-sm m-2 p-0 d-flex flex-column">
                <h4 className="p-2 d-flex justify-content-center">All products</h4>
                <Accordion open={open} toggle={toggleAccordion}>
                    {products.map((item) => (
                        <AccordionItem key={item.id}>
                            <AccordionHeader targetId={item.id.toString()}>
                                <div className="d-flex justify-content-between w-100 me-3">
                                    {item.name}
                                    <Button color="danger" onClick={() => removeProduct(item.id)} className="ms-auto">
                                        <i className="bi-trash"></i>
                                    </Button>
                                </div>
                            </AccordionHeader>
                            <AccordionBody accordionId={item.id.toString()}>
                                {variations ? (
                                    variations.map((variation) => (
                                        <Row key={variation.id} className="mb-2 align-items-center">
                                            <Col xs="auto">
                                                <img
                                                    src={variation.picture}
                                                    alt={variation.name}
                                                    style={{ width: "50px" }}
                                                />
                                            </Col>
                                            <Col>
                                                <span>{variation.name} - ${variation.price} | Stock: {variation.inventoryQuantity}</span>
                                            </Col>
                                            <Col xs="auto">
                                                <Button color="danger" onClick={() => removeVariant(variation.id)}>
                                                    <i className="bi-trash"></i>
                                                </Button>
                                            </Col>
                                        </Row>
                                    ))
                                ) : (
                                    <p>Loading variations...</p>
                                )}
                            </AccordionBody>
                        </AccordionItem>
                    ))}
                </Accordion>
            </Col>
            <Col className="border rounded shadow-sm m-2 p-4">
                <h4 className="p-2 d-flex justify-content-center">Create new product</h4>
                <div className="d-flex justify-content-center mb-1">
                    <Label className="w-25 d-flex flex-column justify-content-center p-0 m-0">Item name</Label>
                    <Input placeholder="Enter item name"></Input>
                </div>
                <div className="d-flex justify-content-center mb-1">
                    <Label className="w-25 d-flex flex-column justify-content-center p-0 m-0">Item price</Label>
                    <Input placeholder="Enter item price"></Input>
                </div>
                <div className="d-flex justify-content-center mt-5">
                    <Button color="success" className="me-3">
                        Create
                    </Button>
                    <Button color="danger">
                        Clear
                    </Button>
                </div>
            </Col>
        </Row>
    );
}

export default Products;