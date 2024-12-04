import {Accordion, AccordionBody, AccordionHeader, AccordionItem, Button, Col, Input, Label, Row} from "reactstrap";
import {useEffect, useState} from "react";
import * as discountApi from "@/api/discountApi.jsx";
import * as ProductApi from "@/api/productApi.jsx";


const Discount = () => {
    const [isLoading, setIsLoading] = useState(true);

    const [discounts, setDiscounts] = useState([]);
    const [variations, setVariations] = useState([]);

    const [discountAmount, setDiscountAmount] = useState('');
    const [isDiscountPercentage, setIsDiscountPercentage] = useState(true);
    const [discountStartDate, setDiscountStartDate] = useState(null);
    const [discountEndDate, setDiscountEndDate] = useState(null);

    const [open, setOpen] = useState('');

    useEffect( () => {
        fetchDiscounts();
    },[]);

    const fetchDiscounts = async () => {
        const fetchedDiscounts = await discountApi.getDiscounts();
        setDiscounts(fetchedDiscounts);
        setIsLoading(false);
    };

    //temp
    const fetchProductVariations = async (id) => {
        const HACKID = 2;
        const fetchedProductVariations = await ProductApi.getProductVariations(HACKID);
        setVariations(fetchedProductVariations);
    };
    const toggleAccordion = async (id) => {
        if (open === id) {
            setOpen('');
            setVariations([])
        } else {
            setOpen(id);
            await fetchProductVariations(id);
        }
    };

    const removeDiscount = async (id) => {
        //await discountApi.deleteDiscount(id);
        setDiscounts(discounts.filter(discount => discount.id !== id));
    }
    
    const addVariationToDiscount = async (id) => {
        
    }
    
    const confirmAddVariationToDiscount = () => {
      
    }

    const handleCreateDiscount = async () => {}

    const handleClearDiscount = async () => {}

    if (isLoading) {
        return <div>Loading...</div>;
    }

    return (
        <Row style={{ height: "85vh" }}>
            <Col className="border rounded shadow-sm m-2 p-0 d-flex flex-column">
                <h4 className="p-2 d-flex justify-content-center">All discounts</h4>
                <Accordion open={open} toggle={toggleAccordion}>
                    {discounts.map((discount) => (
                        <AccordionItem key={discount.id}>
                            <AccordionHeader targetId={discount.id.toString()}>
                                <div className="d-flex justify-content-between w-100 me-3">
                                    {discount.amount} {discount.isPercentage ? '%' : '$'} discount until {discount.endDate}
                                    <Button
                                        color="danger"
                                        onClick={() => removeDiscount(discount.id)}
                                        className="ms-auto"
                                    >
                                        <i className="bi-trash"></i>
                                    </Button>
                                </div>
                            </AccordionHeader>
                            <AccordionBody accordionId={discount.id.toString()}>
                                {variations.map((variation) => (
                                    <div key={variation.id}>
                                        <Row className="mb-2 align-items-center">
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
                                                <Button color="danger" onClick={() => addVariationToDiscount(variation.id)}>
                                                    <i className="bi-plus"></i>
                                                </Button>
                                            </Col>
                                        </Row>
                                    </div>
                                ))}
                                <Row>
                                    <Button color="secondary" outline onClick={() => confirmAddVariationToDiscount()}>
                                        Confirm update
                                    </Button>
                                </Row>
                            </AccordionBody>
                        </AccordionItem>
                    ))}
                </Accordion>
            </Col>
            <Col className="border rounded shadow-sm m-2 p-4">
                <h4 className="p-2 d-flex justify-content-center">Create new discount</h4>
                <div className="d-flex justify-content-center mb-1">
                    <Label className="w-25 d-flex flex-column justify-content-center p-0 m-0">Item name</Label>
                    <Input placeholder="Enter discount amount"
                           value={discountAmount}
                           onChange={(e) => setDiscountAmount(e.target.value)}>
                    </Input>
                </div>
                <div className="d-flex justify-content-center mb-1">
                    <Label className="w-25 d-flex flex-column justify-content-center p-0 m-0">Item group</Label>
                    <Input placeholder="Enter discount start date"
                           value={discountStartDate}
                           onChange={(e) => setDiscountStartDate(e.target.value)}>
                    </Input>
                </div>
                <div className="d-flex justify-content-center mt-5">
                    <Button color="success" className="me-3" onClick={handleCreateDiscount}>
                        Create
                    </Button>
                    <Button color="danger" onClick={handleClearDiscount}>
                        Clear
                    </Button>
                </div>
            </Col>
        </Row>
    );
}

export default Discount;