// wwwroot/js/map-interop.js
// Blazor tarafından çağrılan Leaflet harita fonksiyonları.

window.mapInterop = {
    map: null,
    markers: [],
    userMarker: null,
    tileLayer: null,
    isDark: false,

    // Renk paleti — marka isminden tutarlı bir renk üretir (marka listesini hardcode etmeden)
    _palette: ['#ff4d00', '#0057ff', '#00a651', '#7b2ff7', '#009688', '#e91e63', '#ff9800', '#3f51b5', '#795548', '#607d8b'],

    brandColor: function (brand) {
        if (!brand) return '#555555';
        let hash = 0;
        for (let i = 0; i < brand.length; i++) {
            hash = brand.charCodeAt(i) + ((hash << 5) - hash);
        }
        const idx = Math.abs(hash) % this._palette.length;
        return this._palette[idx];
    },

    init: function (elementId, centerLat, centerLng, zoom) {
        if (this.map) {
            this.map.remove();
        }

        this.map = L.map(elementId, { zoomControl: false }).setView([centerLat, centerLng], zoom);

        this.tileLayer = L.tileLayer(
            'https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png',
            { attribution: '&copy; OpenStreetMap &copy; CARTO', subdomains: 'abcd', maxZoom: 20 }
        ).addTo(this.map);

        L.control.zoom({ position: 'bottomright' }).addTo(this.map);
        this.isDark = false;
    },

    toggleDarkMode: function () {
        if (!this.map) return false;
        this.isDark = !this.isDark;

        this.map.removeLayer(this.tileLayer);
        const url = this.isDark
            ? 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png'
            : 'https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png';

        this.tileLayer = L.tileLayer(url, {
            attribution: '&copy; OpenStreetMap &copy; CARTO',
            subdomains: 'abcd',
            maxZoom: 20
        }).addTo(this.map);

        return this.isDark;
    },

    clearMarkers: function () {
        this.markers.forEach(m => this.map.removeLayer(m));
        this.markers = [];
    },

    addStationMarker: function (id, lat, lng, brand, name, statusColor, dotNetRef) {
        if (!this.map) return;

        const initials = (brand || '??').substring(0, 2).toUpperCase();
        const color = this.brandColor(brand);

        const icon = L.divIcon({
            className: 'station-marker-wrapper',
            html: `
                <div class="station-pin" style="background:${color}">
                    <span>${initials}</span>
                    <span class="status-dot" style="background:${statusColor}"></span>
                </div>
                <div class="station-pin-tail" style="border-top-color:${color}"></div>
            `,
            iconSize: [40, 50],
            iconAnchor: [20, 48],
            popupAnchor: [0, -46]
        });

        const marker = L.marker([lat, lng], { icon }).addTo(this.map);
        marker.on('click', () => {
            if (dotNetRef) dotNetRef.invokeMethodAsync('OnMarkerClicked', id);
        });

        this.markers.push(marker);
    },

    flyTo: function (lat, lng, zoom) {
        if (!this.map) return;
        this.map.flyTo([lat, lng], zoom || 15, { duration: 1 });
    },

    locateUser: function (dotNetRef) {
        if (!navigator.geolocation) {
            dotNetRef.invokeMethodAsync('OnLocationError', 'Geolocation is not supported by this browser.');
            return;
        }

        navigator.geolocation.getCurrentPosition(
            pos => {
                const lat = pos.coords.latitude;
                const lng = pos.coords.longitude;

                if (this.userMarker) this.map.removeLayer(this.userMarker);

                this.userMarker = L.circleMarker([lat, lng], {
                    radius: 9,
                    color: '#ffffff',
                    weight: 3,
                    fillColor: '#1976d2',
                    fillOpacity: 1
                }).addTo(this.map);

                this.map.flyTo([lat, lng], 14, { duration: 1 });
                dotNetRef.invokeMethodAsync('OnLocationFound', lat, lng);
            },
            err => {
                dotNetRef.invokeMethodAsync('OnLocationError', err.message);
            },
            { enableHighAccuracy: true, timeout: 10000 }
        );
    },

    destroy: function () {
        if (this.map) {
            this.map.remove();
            this.map = null;
        }
        this.markers = [];
        this.userMarker = null;
    }
};