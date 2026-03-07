import { Router, Route } from "@solidjs/router";
import ViewerPage from "./pages/ViewerPage";
import HomePage from "./pages/HomePage";

const App = () => {
  return (
    <Router>
      <Route path="/" component={HomePage} />
      <Route path="/registry/:id" component={ViewerPage} />
    </Router>
  );
};

export default App;
