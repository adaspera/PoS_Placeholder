import { Container } from 'reactstrap';
import NavMenu from "@/components/NavMenu.jsx";

const Layout = (props) => {
    return (
        <div>
            <NavMenu/>
            <Container fluid className="m-2" tag="main">
                {props.children}
            </Container>
        </div>
    );
}

export default Layout;