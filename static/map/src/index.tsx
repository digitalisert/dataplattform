import React from 'react';
import ReactDOM from 'react-dom';
import App from './App';
import 'leaflet/dist/leaflet.css';

var root = document.getElementById('map');

if (root) {
    const resources = [...root.querySelectorAll("a")].map(a => a.dataset.resource);
    ReactDOM.render(<App { ...{ resources, ...(root.dataset) } }/>, root);
}
