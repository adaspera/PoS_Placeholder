import {Collapse, Nav, Navbar, NavbarBrand, NavbarToggler, NavItem, NavLink} from 'reactstrap';
import {useState} from "react";
import {Link} from "react-router-dom";


const NavMenu = () => {

    const [isCollapsed, setCollapsed] = useState(true);


    return (
    <header>
        <Navbar className="navbar-expand-sm navbar-toggleable-sm ng-white border-bottom bg-white">
            <NavbarBrand tag={Link} to="/">
                <img src="/ico.png" alt="PineApp" style={{height: '50px'}}/>
            </NavbarBrand>
            <NavbarToggler onClick={() => setCollapsed(!isCollapsed)} className="mr-2" />
            <Collapse isOpen={!isCollapsed} navbar>
                <Nav className="me-auto" navbar>
                    <NavLink tag={Link} to="/business" className="text-dark d-flex flex-column align-items-center" style={{width: "80px"}}>
                        <i className="bi-briefcase" style={{fontSize: '20px'}}/>
                        <div>Business</div>
                    </NavLink>
                    <NavLink tag={Link} to="/tax" className="text-dark d-flex flex-column align-items-center" style={{width: "80px"}}>
                        <i className="bi-bank" style={{fontSize: '20px'}}/>
                        <div>Tax</div>
                    </NavLink>
                    <NavLink tag={Link} to="/products" className="text-dark d-flex flex-column align-items-center" style={{width: "80px"}}>
                        <i className="bi-box-seam" style={{fontSize: '20px'}}/>
                        <div>Products</div>
                    </NavLink>
                    <NavLink tag={Link} to="/orders" className="text-dark d-flex flex-column align-items-center" style={{width: "80px"}}>
                        <i className="bi-receipt" style={{fontSize: '20px'}}/>
                        <div>Orders</div>
                    </NavLink>
                </Nav>
                <Nav className="ms-auto">
                    <NavItem>
                        <NavLink tag={Link} to="/login" className="text-dark">
                            Login
                        </NavLink>
                    </NavItem>
                </Nav>
            </Collapse>
        </Navbar>
    </header>
    );
}

export default NavMenu;