import {useEffect, useState} from "react";
import {
    Accordion,
    AccordionBody,
    AccordionHeader,
    AccordionItem,
    Button,
    Col, Form, FormGroup,
    Input,
    Label,
    Row
} from "reactstrap";
import * as ProductApi from "@/api/productApi.jsx";

const Products = () => {
    const [isLoading, setIsLoading] = useState(true);

    const [products, setProducts] = useState([]);
    const [variations, setVariations] = useState([]);

    const [itemName, setItemName] = useState('');
    const [itemGroup, setItemGroup] = useState('');

    const [variationName, setVariationName] = useState('');
    const [variationPrice, setVariationPrice] = useState('');
    const [variationImage, setVariationImage] = useState({});

    const [isVariationFormOpen, setIsVariationFormOpen] = useState(false);

    const fetchProducts = async () => {
        const fetchedProducts = await ProductApi.getProducts();
        setProducts(fetchedProducts);
        setIsLoading(false);
    };

    const fetchProductVariations = async (id) => {
        const fetchedProductVariations = await ProductApi.getProductVariations(id);
        setVariations(fetchedProductVariations);
    };


    useEffect( () => {
        fetchProducts()
    },[]);


    const [open, setOpen] = useState(null);

    const toggleAccordion = async (id) => {
        if (open === id) {
            setOpen(null);
            setVariations([])
        } else {
            setOpen(id);
            setIsVariationFormOpen(false);
            handleClearVariation();
            await fetchProductVariations(id);
        }
    };

    const removeProduct = async (id) => {
        await ProductApi.deleteProduct(id);
        setProducts(products.filter(product => product.id !== id));
    }

    const removeVariation = async (id) => {
        await ProductApi.deleteProductVariation(id);
        setVariations(variations.filter(variation => variation.id !== id));
    }

    const addProduct = async () => {
        const newProduct = new FormData();
        newProduct.append("name", itemName);
        newProduct.append("itemGroup", itemGroup);
        const createdProduct = await ProductApi.addProduct(newProduct);
        setProducts([...products, createdProduct]);
    };

    const addVariation = async (productId) => {
        const newVariation = new FormData();
        newVariation.append("name", variationName);
        newVariation.append("price", variationPrice);
        newVariation.append("pictureFile", variationImage);
        newVariation.append("productId", productId);
        const createdVariation = await ProductApi.addProductVariation(newVariation);

        setVariations([...variations, createdVariation]);
    }

    const handleClearProduct = () => {
        setItemName('');
        setItemGroup('');
    };

    const handleClearVariation = () => {
        setVariationName('');
        setVariationPrice('');
        setVariationImage(null);
    }

    const variationForm = (id) => {
        return isVariationFormOpen ?
            <Row className="border rounded p-3 ps-4">
                <Form>
                    <FormGroup row>
                        <Label sm={3}>Variation name</Label>
                        <Col sm={9}>
                            <Input value={variationName}
                                   onChange={(e) => setVariationName(e.target.value)}>
                            </Input>
                        </Col>
                    </FormGroup>
                    <FormGroup row>
                        <Label sm={3}>Variation price</Label>
                        <Col sm={9}>
                            <Input value={variationPrice}
                                   onChange={(e) => setVariationPrice(e.target.value)}>
                            </Input>
                        </Col>
                    </FormGroup>
                    <FormGroup row>
                        <Label sm={3}>Picture</Label>
                        <Col sm={9}>
                            <Input type="file"
                                   onChange={(e) => setVariationImage(e.target.files[0])}>
                            </Input>
                        </Col>
                    </FormGroup>
                </Form>
                <Col>
                    <Button color="success" className="me-3" onClick={() => addVariation(id)}>Create</Button>
                    <Button color="danger" onClick={handleClearVariation}>Cancel</Button>
                </Col>
            </Row>
            :
            <Row>
                <Button color="secondary" outline onClick={() => setIsVariationFormOpen(true)}>
                    Add variation
                </Button>
            </Row>
    }

    if (isLoading) {
        return <div>Loading...</div>;
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
                                                    src={variation.pictureUrl}
                                                    alt={variation.name}
                                                    style={{ width: "50px" }}
                                                />
                                            </Col>
                                            <Col>
                                                <span>{variation.name} - ${variation.price} | Stock: {variation.inventoryQuantity}</span>
                                            </Col>
                                            <Col xs="auto">
                                                <Button color="danger" onClick={() => removeVariation(variation.id)}>
                                                    <i className="bi-trash"></i>
                                                </Button>
                                            </Col>
                                        </Row>
                                    ))
                                ) : (
                                    <p>Loading variations...</p>
                                )}
                                {variationForm(item.id)}
                            </AccordionBody>
                        </AccordionItem>
                    ))}
                </Accordion>
            </Col>
            <Col className="border rounded shadow-sm m-2 p-4">
                <h4 className="p-2 d-flex justify-content-center">Create new product</h4>
                <div className="d-flex justify-content-center mb-1">
                    <Label className="w-25 d-flex flex-column justify-content-center p-0 m-0">Item name</Label>
                    <Input placeholder="Enter item name"
                        value={itemName}
                        onChange={(e) => setItemName(e.target.value)}>
                    </Input>
                </div>
                <div className="d-flex justify-content-center mb-1">
                    <Label className="w-25 d-flex flex-column justify-content-center p-0 m-0">Item group</Label>
                    <Input placeholder="Enter item group"
                           value={itemGroup}
                           onChange={(e) => setItemGroup(e.target.value)}>
                    </Input>
                </div>
                <div className="d-flex justify-content-center mt-5">
                    <Button color="success" className="me-3" onClick={addProduct}>
                        Create
                    </Button>
                    <Button color="danger" onClick={handleClearProduct}>
                        Clear
                    </Button>
                </div>
            </Col>
        </Row>
    );
}

export default Products;