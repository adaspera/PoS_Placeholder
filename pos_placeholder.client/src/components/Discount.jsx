import {Accordion, AccordionBody, AccordionHeader, AccordionItem, Button, Col, Input, Label, Row} from "reactstrap";
import {useEffect, useState} from "react";
import * as discountApi from "@/api/discountApi.jsx";
import * as ProductApi from "@/api/productApi.jsx";
import {getCurrency} from "@/helpers/currencyUtils.jsx";
import toastNotify from "@/helpers/toastNotify.js";


const Discount = () => {
    const [isLoading, setIsLoading] = useState(true);

    const [discounts, setDiscounts] = useState([]);
    const [variations, setVariations] = useState([]);
    const [notIncludedVariations, setNotIncludedVariations] = useState([]);

    const [discountAmount, setDiscountAmount] = useState('');
    const [isDiscountPercentage, setIsDiscountPercentage] = useState(true);
    const [discountStartDate, setDiscountStartDate] = useState(null);
    const [discountEndDate, setDiscountEndDate] = useState(null);

    const [changeLogs, setChangeLogs] = useState([]);

    const [open, setOpen] = useState('');

    useEffect( () => {
        fetchDiscounts();
    },[]);

    const fetchDiscounts = async () => {
        const fetchedDiscounts = await discountApi.getDiscounts();
        setDiscounts(fetchedDiscounts);
        setIsLoading(false);
    };

    const fetchIncludedProductVariations = async (id) => {
        const fetchedProductVariations = await discountApi.getVariationsByDiscountId(id);
        setVariations(fetchedProductVariations);
    };

    const fetchNotIncludedProductVariations = async (id) => {
        let fetchedProductVariations = await ProductApi.getAllProductVariations(id);
        fetchedProductVariations = fetchedProductVariations.filter(pv => pv.discountId !== Number(id));
        setNotIncludedVariations(fetchedProductVariations);
    };

    const toggleAccordion = async (id) => {
        if (open === id) {
            setOpen('');
            setVariations([]);
            setNotIncludedVariations([]);
        } else {
            setOpen(id);
            await fetchIncludedProductVariations(id);
            await fetchNotIncludedProductVariations(id)
        }
    };

    const removeDiscount = async (id) => {
        await discountApi.deleteDiscount(id);
        setDiscounts(discounts.filter(discount => discount.id !== id));
        toastNotify("Discount successfully delete", "warning");
    }
    
    const addVariationToDiscount = async (id) => {
        const variation = notIncludedVariations.find(variation => variation.id === id);
        setChangeLogs([...changeLogs, {productVariationId: id, isAdd: true} ]);
        setVariations([...variations, variation]);
        setNotIncludedVariations(notIncludedVariations.filter(variation => variation.id !== id));
    }

    const removeVariationFromDiscount = async (id) => {
        const variation = variations.find(variation => variation.id === id);
        setChangeLogs([...changeLogs, {productVariationId: id, isAdd: false} ]);
        setNotIncludedVariations([...notIncludedVariations, variation]);
        setVariations(variations.filter(variation => variation.id !== id));
    }
    
    const confirmAddVariationToDiscount = async (id) => {
        await discountApi.addProductVariationsToDiscount(id, changeLogs);
        setChangeLogs([]);
        setOpen('');
        toastNotify("Changes saved!", "success");
    }

    const handleCreateDiscount = async () => {
        try{
            const newDiscount = {
                amount: discountAmount,
                startDate: discountStartDate,
                endDate: discountEndDate,
                isPercentage: isDiscountPercentage
            }

            const createdDiscount = await discountApi.createDiscount(newDiscount);
            setDiscounts([...discounts, createdDiscount]);
            toastNotify("New discount created!", "success");
        } catch (e) {
            toastNotify("Please provide all the fields correctly.", "error");
        }
    }

    const handleClearDiscount = async () => {
        setDiscountAmount('');
        setDiscountStartDate('');
        setDiscountEndDate('');
        setIsDiscountPercentage(true);
    }

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
                                    {discount.amount} {discount.isPercentage ? '%' : getCurrency()} discount &#32;
                                    {new Date(discount.startDate).toLocaleDateString()} to {new Date(discount.endDate).toLocaleDateString()}
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
                                <Row className="pb-2">
                                    <Button color="secondary" outline onClick={() => confirmAddVariationToDiscount(discount.id)}>
                                        Save changes
                                    </Button>
                                </Row>
                                <h5>Discounted products</h5>
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
                                                <span>{variation.name} - {getCurrency()}{variation.price} | Stock: {variation.inventoryQuantity}</span>
                                            </Col>
                                            <Col xs="auto">
                                                <Button color="danger" onClick={() => removeVariationFromDiscount(variation.id)}>
                                                    <i className="bi-file-minus"></i>
                                                </Button>
                                            </Col>
                                        </Row>
                                    </div>
                                ))}
                                <h5>Available products</h5>
                                {notIncludedVariations.map((variation) => (
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
                                                <span>{variation.name} - {getCurrency()}{variation.price} | Stock: {variation.inventoryQuantity}</span>
                                            </Col>
                                            <Col xs="auto">
                                                <Button color="danger" onClick={() => addVariationToDiscount(variation.id)}>
                                                    <i className="bi-plus"></i>
                                                </Button>
                                            </Col>
                                        </Row>
                                    </div>
                                ))}
                            </AccordionBody>
                        </AccordionItem>
                    ))}
                </Accordion>
            </Col>
            <Col className="border rounded shadow-sm m-2 p-4">
                <h4 className="p-2 d-flex justify-content-center">Create new discount</h4>
                <div className="d-flex justify-content-center mb-1">
                    <Label className="w-25 d-flex flex-column justify-content-center p-0 m-0">Discount amount</Label>
                    <Input placeholder="Enter discount amount"
                           value={discountAmount}
                           onChange={(e) => setDiscountAmount(e.target.value)}>
                    </Input>
                </div>
                <div className="d-flex justify-content-left align-items-center mb-1">
                    <Label className="w-25 p-0 m-0">Discount is percentage</Label>
                    <Input checked={isDiscountPercentage}
                           onChange={(e) => setIsDiscountPercentage(e.target.checked)}
                           type="checkbox"
                           className="ms-1"
                    />
                </div>
                <div className="d-flex justify-content-center mb-1">
                    <Label className="w-25 d-flex flex-column justify-content-center p-0 m-0">Start date</Label>
                    <Input type="date"
                           value={discountStartDate}
                           onChange={(e) => setDiscountStartDate(e.target.value)}>
                    </Input>
                </div>
                <div className="d-flex justify-content-center mb-1">
                    <Label className="w-25 d-flex flex-column justify-content-center p-0 m-0">End date</Label>
                    <Input type="date"
                           value={discountEndDate}
                           onChange={(e) => setDiscountEndDate(e.target.value)}>
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