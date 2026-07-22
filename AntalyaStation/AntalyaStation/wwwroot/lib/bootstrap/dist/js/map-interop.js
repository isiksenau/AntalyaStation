window.mapInterop = {
    map: null,
    clusterGroup: null,

    init: function (elementId, lat, lng, zoom) {
        if (this.map) {
            this.map.remove();
            this.map = null;
        }

        this.map = L.map(elementId).setView([lat, lng], zoom);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; OpenStreetMap contributors'
        }).addTo(this.map);
    },

    renderMarkersBatch: function (stations, dotNetRef) {
        if (!this.map) return;

        if (this.clusterGroup) {
            this.map.removeLayer(this.clusterGroup);
        }

        if (typeof L.markerClusterGroup === 'function') {
            this.clusterGroup = L.markerClusterGroup({
                chunkedLoading: true,
                maxClusterRadius: 44,
                spiderfyOnMaxZoom: true,
                showCoverageOnHover: false,
                // 🟢 Kurumsal, tek renk, temiz daire cluster ikonu
                iconCreateFunction: function (cluster) {
                    const count = cluster.getChildCount();
                    const size = count < 10 ? 36 : count < 50 ? 44 : 52;
                    return L.divIcon({
                        html: `<div class="station-cluster"><span>${count}</span></div>`,
                        className: '',
                        iconSize: [size, size],
                        iconAnchor: [size / 2, size / 2]
                    });
                }
            });
        } else {
            this.clusterGroup = L.layerGroup();
        }

        let markersToAdd = [];

        stations.forEach(s => {
            let iconHtml = `<div class="station-pin" style="background-color: ${s.color};">
                                ${s.initials}
                            </div>`;

            let customIcon = L.divIcon({
                html: iconHtml,
                className: '',
                iconSize: [32, 32],
                iconAnchor: [16, 16]
            });

            let marker = L.marker([s.lat, s.lng], { icon: customIcon });

            let popupContent = `
                <div class="popup-content">
                    <div class="popup-brand" style="color: ${s.color};">${s.brand}</div>
                    <div class="popup-code">#${s.code}</div>
                    <div class="popup-status">${s.status}</div>
                    <div class="popup-district">${s.district}</div>
                </div>
            `;
            marker.bindPopup(popupContent, { className: 'custom-popup' });

            marker.on('click', function () {
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnMarkerClicked', s.id);
                }
            });

            markersToAdd.push(marker);
        });

        this.clusterGroup.addLayers(markersToAdd);
        this.map.addLayer(this.clusterGroup);
    },

    clearMarkers: function () {
        if (this.clusterGroup) {
            this.clusterGroup.clearLayers();
        }
    },

    flyTo: function (lat, lng, zoom) {
        if (this.map) {
            this.map.flyTo([lat, lng], zoom);
        }
    },

    toggleDarkMode: function () {
        return false;
    },

    locateUser: function (dotNetRef) {
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(
                position => {
                    let lat = position.coords.latitude;
                    let lng = position.coords.longitude;
                    if (dotNetRef) dotNetRef.invokeMethodAsync('OnLocationFound', lat, lng);
                },
                error => {
                    if (dotNetRef) dotNetRef.invokeMethodAsync('OnLocationError', error.message);
                }
            );
        }
    },

    destroy: function () {
        if (this.map) {
            this.map.remove();
            this.map = null;
        }
    }
};