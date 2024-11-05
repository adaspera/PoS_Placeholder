import { Container } from 'reactstrap';

const Layout = (props) => {
    return (
        <div>
            <Container fluid className="m-2" tag="main">
                {props.children}
            </Container>
        </div>
    );
}

export default Layout;