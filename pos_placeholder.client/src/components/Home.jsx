import {
    Button,
    Col,
    Input,
    Label,
    Modal,
    ModalBody,
    ModalFooter,
    ModalHeader,
    Row,
    Form,
    FormGroup,
    FormFeedback
} from "reactstrap";
import {useEffect, useState} from "react";
import * as productApi from "@/api/productApi.jsx";
import * as orderApi from "@/api/orderApi.jsx";
import * as paymentApi from "@/api/paymentApi.jsx";
import * as discountApi from "@/api/discountApi.jsx";
import {getCurrency} from "@/helpers/currencyUtils.jsx";
import Payment from "@/components/payment/payment.jsx";
import Giftcard from "@/components/shared/Giftcard.jsx";
import toastNotify from "@/helpers/toastNotify.js";
import {createSplitPaymentOrder} from "@/api/orderApi.jsx";

const Home = () => {
    const [isLoading, setIsLoading] = useState(true);

    const [tip, setTip] = useState('');
    const [orderPreview, setOrderPreview] = useState({});
    const [order, setOrder] = useState({products: []});
    const [paymentData, setPaymentData] = useState({});
    const [selectedPaymentMethod, setSelectedPaymentMethod] = useState(null);

    const [totalPrice, setTotalPrice] = useState("0");
    const [products, setProducts] = useState(null);
    const [variations, setVariations] = useState([]);
    const [productsInCart, setProductsInCart] = useState(null);
    const [productsInCatalogue, setProductsInCatalogue] = useState(null);

    const [selectedProduct, setSelectedProduct] = useState(null);
    const [paySelected, setPaySelected] = useState(false);

    const [splitCheckSelected, setSplitCheckSelected] = useState(false);
    const [totalRemaining, setTotalRemaining] = useState(0);
    const [partialPaymentSelectedAmount, setPartialPaymentSelectedAmount] = useState(0.01);
    const [isValidAmount, setIsValidAmount] = useState(true);
    const [amountErrorMessage, setAmountErrorMessage] = useState("")
    const [partialPaymentLocked, setPartialPaymentLocked] = useState(false);
    const [partialPayments, setPartialPayments] = useState([]);


    const fetchProducts = async () => {
        try {
            const fetchedProducts = await productApi.getProducts();
            setProducts(fetchedProducts);
        } catch (error) {
            console.error("Error fetching products:", error);
        } finally {
            setIsLoading(false);
        }
    };

    const fetchProductVariations = async (id) => {
        const fetchedProductVariations = await productApi.getProductVariations(id);
        setVariations(fetchedProductVariations);
    };

    useEffect(() => {
        fetchProducts();
    }, []);

    useEffect(() => {
        if (products && products.length > 0) {
            formatProductsInCatalogue(products);
        }
    }, [products]);

    useEffect(() => {
        formatProductsInCart();
        const total = order.products.reduce((acc, item) => acc + item.price * item.quantity, 0);
        setTotalPrice(total.toFixed(2));
    }, [order]);

    const formatProductsInCart = () => {
        const formatedProductsInCart = order.products.map((item, index) => (
            <Row key={index} className="p-2">
                <Col>{item.fullName}</Col>
                <Col className="d-flex justify-content-center">x{item.quantity}</Col>
                <Col className="d-flex justify-content-end">
                    {item.isDiscountPercentage ? (
                        <>
                            <span style={{textDecoration: "line-through"}}>
                                {item.price} {getCurrency()}
                            </span>
                            &nbsp;
                            {item.discount} {getCurrency()}
                        </>
                    ) : (
                        <>
                            {item.price}{item.discount === null ? "" : " -"}{item.discount} {getCurrency()}
                        </>
                    )}
                    <i
                        className="bi-x-circle px-2"
                        style={{cursor: "pointer"}}
                        onClick={() => handleRemoveFromCart(item.productVariationId)}
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
                {Array.from({length: Math.ceil(groupProducts.length / itemsPerRow)}, (_, rowIndex) => {
                    const rowItems = groupProducts.slice(rowIndex * itemsPerRow, (rowIndex + 1) * itemsPerRow);

                    return (
                        <Row key={rowIndex} className="pb-4">
                            {rowItems.map((item) => (
                                <Col key={item.id} md={12 / itemsPerRow} xs={6}>
                                    <div className="border rounded p-2" style={{cursor: "pointer"}}
                                         onClick={() => handleProductClick(item)}>
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
    };

    const handleProductClick = async (product) => {
        setSelectedProduct(product);
        await fetchProductVariations(product.id);
    };

    const handleAddToCart = async (variation, product) => {
        let discountedPrice = null;
        let isDiscountPercentage = null;
        if (variation.discountId) {
            const discount = await discountApi.getDiscount(variation.discountId);
            if (discount) {
                isDiscountPercentage = discount.isPercentage;
                discountedPrice = discount.isPercentage ? variation.price - variation.price * discount.amount / 100 : discount.amount;
                discountedPrice = discountedPrice.toFixed(2);
            }
        }

        const newProductInCart = {
            productVariationId: variation.id,
            fullName: product.name + " " + variation.name,
            price: variation.price,
            quantity: 1,
            discount: discountedPrice,
            isDiscountPercentage: isDiscountPercentage
        };

        const existingProductIndex = order.products.findIndex(item => item.productVariationId === variation.id);
        let updatedProductsInCart;
        if (existingProductIndex === -1) {
            updatedProductsInCart = [...order.products, newProductInCart];
        } else {
            updatedProductsInCart = order.products.map((item, index) => {
                if (index === existingProductIndex) {
                    return {...item, quantity: item.quantity + 1};
                }
                return item;
            });
        }

        setOrder(prevOrder => ({...prevOrder, products: updatedProductsInCart}));
    };

    const handleRemoveFromCart = (productVariationId) => {
        const existingIndex = order.products.findIndex(item => item.productVariationId === productVariationId);
        if (existingIndex === -1) {
            return;
        }

        const existingItem = order.products[existingIndex];
        let updatedProducts;

        if (existingItem.quantity > 1) {
            updatedProducts = order.products.map((item, index) => {
                if (index === existingIndex) {
                    return {...item, quantity: item.quantity - 1};
                }
                return item;
            });
        } else {
            updatedProducts = order.products.filter(item => item.productVariationId !== productVariationId);
        }

        setOrder((prevOrder) => ({...prevOrder, products: updatedProducts}));
    };

    const modal = (
        <Modal isOpen={!!selectedProduct} fade={false} size="lg" centered={true}>
            <ModalBody>
                <h5>{selectedProduct?.name}</h5>
                {variations.map((variation) => (
                    <div key={variation.id} className="p-2 border rounded mb-2"
                         style={{cursor: "pointer"}}
                         onClick={() => handleAddToCart(variation, selectedProduct)}>
                        <h6>{variation.name}</h6>
                        <p>Price: {variation.price} {getCurrency()}</p>
                        {variation.pictureUrl && <img src={variation.pictureUrl} alt={variation.name} width="50"/>}
                    </div>
                ))}
            </ModalBody>
            <ModalFooter>
                <Button color="secondary" onClick={() => setSelectedProduct(null)}>
                    Cancel
                </Button>
            </ModalFooter>
        </Modal>
    );

    const handlePayNowClick = async () => {
        if (order.products.length === 0) {
            toastNotify("Your cart is empty! Add some items...", "warning")
            return;
        }

        const createOrderDto = {
            Tip: tip ? Number(tip) : null,
            OrderItems: order.products.map(item => ({
                ProductVariationId: item.productVariationId,
                Quantity: item.quantity
            })),
            PaymentIntentId: null,
            GiftCardId: null,
            Method: null
        };

        const orderPreviewResponse = await orderApi.getOrderPreview(createOrderDto);
        setOrderPreview(orderPreviewResponse);

        setPaySelected(true);
    };

    const handleSplitCheckClick = () => {
        setTotalRemaining(orderPreview.total)
        setPaySelected(false);
        setSelectedPaymentMethod(null);
        setSplitCheckSelected(true);
    }

    const handleCardPayment = async () => {
        const paymentRequestDto = {
            TotalAmount: orderPreview.total
        }
        const paymentResponse = await paymentApi.makePayment(paymentRequestDto);
        setPaymentData(paymentResponse);
        setSelectedPaymentMethod('card');
    };

    const handleCashPayment = async () => {
        const createOrderDto = {
            Tip: tip ? Number(tip) : null,
            OrderItems: order.products.map(item => ({
                ProductVariationId: item.productVariationId,
                Quantity: item.quantity
            })),
            PaymentIntentId: null,
            GiftCardId: null,
            Method: 2 // 0 -> "card", 1 -> "giftcard", 2 -> "cash"
        };

        const createdOrder = await orderApi.createOrder(createOrderDto);

        onPaymentSuccess();
    };

    const onPaymentSuccess = () => {
        setSelectedPaymentMethod(null);
        setPaySelected(false);
        setOrder({products: []});
        toastNotify("Order successfully created!", "success");
    };

    const paymentModal = (
        <Modal isOpen={paySelected} fade={true} size="lg" centered={true}>
            <ModalHeader>
                Checkout
            </ModalHeader>
            <ModalBody>
                <div>
                    <div className="d-flex justify-content-between">
                        <span>Subtotal:</span>
                        <span>{orderPreview.subTotal} {getCurrency()}</span>
                    </div>
                    <div className="d-flex justify-content-between">
                        <span>Taxes:</span>
                        <span>{orderPreview.taxesTotal} {getCurrency()}</span>
                    </div>
                    <div className="d-flex justify-content-between">
                        <span>Discounts:</span>
                        <span>{orderPreview.discountsTotal} {getCurrency()}</span>
                    </div>
                    <div className="d-flex justify-content-between">
                        <span>Tip:</span>
                        <span>{orderPreview.tip} {getCurrency()}</span>
                    </div>
                    <hr/>
                    <div className="d-flex justify-content-between fw-bold">
                        <span>Total:</span>
                        <span>{orderPreview.total} {getCurrency()}</span>
                    </div>
                </div>

                <Col>
                    <FormGroup check>
                        <Input
                            name="radio2"
                            type="radio"
                            value="card"
                            checked={selectedPaymentMethod === 'card'}
                            onChange={() => handleCardPayment()}
                        />
                        <Label check>
                            Pay with card
                        </Label>
                    </FormGroup>
                    {selectedPaymentMethod === 'card' && (
                        <div className="my-3">
                            <Payment
                                paymentData={paymentData}
                                order={order}
                                tip={tip}
                                onPaymentSuccess={onPaymentSuccess}
                                isSplitPayment={false}
                            />
                        </div>
                    )}
                    <FormGroup check>
                        <Input
                            name="radio2"
                            type="radio"
                            value="giftcard"
                            checked={selectedPaymentMethod === 'giftcard'}
                            onChange={(e) => setSelectedPaymentMethod('giftcard')}
                        />
                        <Label check>
                            Pay with gift card
                        </Label>
                    </FormGroup>
                    {selectedPaymentMethod === 'giftcard' && (
                        <div className="mt-3">
                            <Giftcard onPaymentSuccess={onPaymentSuccess} order={order} tip={tip}
                                      isSplitPayment={false}/>
                        </div>
                    )}
                </Col>
            </ModalBody>
            <ModalFooter>
                <Button color="danger" className="w-25" onClick={() => {
                    setPaySelected(false);
                    setSelectedPaymentMethod(null);
                }}>
                    Cancel
                </Button>
                <Button color="dark" outline className="w-25"
                        onClick={() => {
                            handleSplitCheckClick();
                        }}
                >
                    Split check
                </Button>
                <Button color="success" className="w-25"
                        onClick={() => {
                            handleCashPayment();
                        }}>
                    Pay with cash
                </Button>
            </ModalFooter>
        </Modal>
    );

    const handlePartialPaymentSuccess = (partialPaymentData) => {
        setPartialPayments(prev => [...prev, partialPaymentData]);
        setTotalRemaining(prev => {
            const newValue = prev - partialPaymentData.PaidPrice;
            return Math.round(newValue * 100) / 100;
        });
        setPartialPaymentSelectedAmount(0.01);
        setSelectedPaymentMethod(null);
        setPartialPaymentLocked(false);
    };

    const handleSplitCheckCancel = () => {
        setSelectedPaymentMethod(null);
        setSplitCheckSelected(false);
        setPartialPayments([]);
        setPartialPaymentSelectedAmount(0.01);
        setIsValidAmount(true);
        setAmountErrorMessage("");
        setPartialPaymentLocked(false);
        setPaymentData(null);
    };

    const handleResetFields = () => {
        setSelectedPaymentMethod(null);
        setPartialPaymentSelectedAmount(0.01);
        setIsValidAmount(true);
        setAmountErrorMessage("");
        setPartialPaymentLocked(false);
        setPaymentData(null);
    };

    const handlePartialAmountChange = (e) => {
        const inputValue = e.target.value;
        let val = parseFloat(inputValue);

        if (isNaN(val)) {
            val = "";
        }

        setPartialPaymentSelectedAmount(val);

        if (val === "" || val === 0) {
            // If empty or zero, invalid
            setIsValidAmount(false);
            setAmountErrorMessage(val === "" ? "Please enter an amount." : "Amount must be greater than zero.");
        } else if (val > totalRemaining) {
            setIsValidAmount(false);
            setAmountErrorMessage("Amount cannot exceed total remaining.");
        } else {
            setIsValidAmount(true);
            setAmountErrorMessage("");
        }
    };

    const handleSplitCardPayment = async () => {
        if (!isValidAmount || partialPaymentSelectedAmount <= 0 || partialPaymentSelectedAmount > totalRemaining) {
            return;
        }

        const paymentRequestDto = {
            TotalAmount: partialPaymentSelectedAmount
        }
        const paymentResponse = await paymentApi.makePayment(paymentRequestDto);
        setPaymentData(paymentResponse);
        setSelectedPaymentMethod('card');
        setPartialPaymentLocked(true);
    };

    const handleSplitCashPayment = () => {
        const partialCashPayment = {
            PaymentIntentId: null,
            GiftCardId: null,
            Method: 2, // 0 -> "card", 1 -> "giftcard", 2 -> "cash"
            PaidPrice: partialPaymentSelectedAmount,
        }

        handlePartialPaymentSuccess(partialCashPayment);
    };

    const handleFinalizeOrder = async () => {
        try {
            const splitOrderDto = {
                Tip: tip ? Number(tip) : null,
                OrderItems: order.products.map((item) => ({
                    ProductVariationId: item.productVariationId,
                    Quantity: item.quantity
                })),
                Payments: partialPayments.map((p) => ({
                    PaymentIntentId: p.PaymentIntentId,
                    GiftCardId: p.GiftCardId,
                    Method: p.Method,
                    PaidPrice: p.PaidPrice
                }))
            };
            
            const createdOrder = await orderApi.createSplitPaymentOrder(splitOrderDto);

            // If success
            toastNotify("Order successfully created!", "success");
            setSplitCheckSelected(false);
            setPartialPayments([]);
            setTotalRemaining(0);
            setPartialPaymentSelectedAmount("");
            setIsValidAmount(true);
            setAmountErrorMessage("");
            setPartialPaymentLocked(false);
            setSelectedPaymentMethod(null);
            setPaymentData(null);
            setOrder({ products: [] });
            setSplitCheckSelected(false);
            setPaySelected(false);
        } catch (error) {
            const backendError = error.message || "Failed to finalize order.";
            toastNotify(backendError, "error");
        }
    };

    const splitCheckModal = (
        <Modal isOpen={splitCheckSelected} fade={true} size="lg" centered={true}>
            <ModalHeader>
                Split Check
            </ModalHeader>
            <ModalBody>
                <div className="mb-2">
                    <div className="d-flex justify-content-between">
                        <span>Total Remaining</span>
                        <span>{totalRemaining.toFixed(2)} {getCurrency()}</span>
                    </div>
                    <hr/>
                    <FormGroup className="d-flex align-items-center mt-2 mb-4">
                        <Label className="me-3 mb-0">Selected Amount:</Label>
                        <div className="flex-grow-1">
                            <Input
                                placeholder="Enter selected amount for partial payment"
                                value={partialPaymentSelectedAmount}
                                invalid={!isValidAmount}
                                type="number"
                                disabled={partialPaymentLocked}
                                onChange={handlePartialAmountChange}
                            />
                            {!isValidAmount && (
                                <FormFeedback className="position-absolute">{amountErrorMessage}</FormFeedback>
                            )}
                        </div>
                    </FormGroup>
                    <div className="d-flex justify-content-between">
                        <span>Partial payment amount selected:</span>
                        <span>{partialPaymentSelectedAmount} {getCurrency()}</span>
                    </div>
                    <hr/>
                </div>
                <Col>
                    <FormGroup check>
                        <Input
                            name="radio2"
                            type="radio"
                            value="card"
                            checked={selectedPaymentMethod === 'card'}
                            disabled={!isValidAmount || partialPaymentLocked}
                            onChange={() => handleSplitCardPayment()}
                        />
                        <Label check>
                            Pay with card
                        </Label>
                    </FormGroup>
                    {selectedPaymentMethod === 'card' && (
                        <div className="my-3">
                            <Payment
                                paymentData={paymentData}
                                order={order}
                                tip={tip}
                                onPaymentSuccess={onPaymentSuccess}
                                isSplitPayment={true}
                                partialAmount={partialPaymentSelectedAmount}
                                onPartialPaymentSuccess={handlePartialPaymentSuccess}
                            />
                        </div>
                    )}
                    <FormGroup check>
                        <Input
                            name="radio2"
                            type="radio"
                            value="giftcard"
                            checked={selectedPaymentMethod === 'giftcard'}
                            disabled={!isValidAmount || partialPaymentLocked}
                            onChange={(e) => {
                                setSelectedPaymentMethod('giftcard');
                                setPartialPaymentLocked(true);
                            }}
                        />
                        <Label check>
                            Pay with gift card
                        </Label>
                    </FormGroup>
                    {selectedPaymentMethod === 'giftcard' && (
                        <div className="mt-3">
                            <Giftcard onPaymentSuccess={onPaymentSuccess} order={order} tip={tip} isSplitPayment={true}
                                      partialAmount={partialPaymentSelectedAmount}
                                      onPartialPaymentSuccess={handlePartialPaymentSuccess}/>
                        </div>
                    )}
                </Col>
            </ModalBody>
            <ModalFooter>
                <Button color="danger" style={{width: 150}} onClick={() => handleSplitCheckCancel()}>
                    Cancel
                </Button>
                <Button color={totalRemaining !== 0 ? `danger` : `success`} disabled={totalRemaining !== 0}
                        style={{width: 150}} onClick={handleFinalizeOrder}>
                    Finalize order
                </Button>
                <Button color="warning" style={{width: 150}} onClick={() => handleResetFields()}>
                    Reset fields
                </Button>
                <Button color="success" style={{width: 150}} disabled={!isValidAmount || partialPaymentLocked}
                        onClick={() => handleSplitCashPayment()}>
                    Pay with cash
                </Button>
            </ModalFooter>
        </Modal>
    );

    if (isLoading) {
        return <div>Loading...</div>;
    }

    return (
        <Row style={{height: "85vh"}}>
            <Col className="border rounded shadow-sm m-2 p-1 d-flex flex-column" lg={4}>
                <div className="justify-content-center border rounded p-2">
                    Total: {totalPrice} {getCurrency()}
                </div>
                <div>
                    {productsInCart}
                </div>
                <div className="d-flex justify-content-center m-2 mt-auto align-items-center">
                    <div className="d-flex justify-content-start col-xl-6 col-lg-4">
                        <Label className="w-25 d-flex flex-column justify-content-center p-0 m-0">Tip:</Label>
                        <Input placeholder="Enter tip value"
                               value={tip}
                               type={"number"}
                               onChange={(e) => setTip(e.target.value)}>
                        </Input>
                    </div>
                    <div className="d-flex justify-content-center col-auto">
                        <Button color="success" className="m-1" onClick={handlePayNowClick}>Pay now</Button>
                    </div>
                </div>
            </Col>
            <Col className="border rounded shadow-sm m-2 p-2">
                {productsInCatalogue}
            </Col>

            {modal}
            {paymentModal}
            {splitCheckModal}
        </Row>
    );
};

export default Home;