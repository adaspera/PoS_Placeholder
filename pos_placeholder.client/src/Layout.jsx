import { Container } from 'reactstrap';
import NavMenu from "@/components/NavMenu.jsx";

const Layout = ({ children, onLogout }) => {
    return (
        <div>
            <NavMenu onLogout={onLogout}/>
            <Container fluid className="px-3" tag="main">
                {children}
            </Container>
        </div>
    );
}

export default Layout;