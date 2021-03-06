import React, { useCallback, useEffect, useState, CSSProperties } from 'react';
import ReactDOM from 'react-dom';
import { MapContainer, Marker, LayersControl, LayerGroup, Polygon, Polyline, Popup, ScaleControl, TileLayer, Tooltip, useMap, WMSTileLayer } from 'react-leaflet';
import L from 'leaflet';
import Wkt from 'wicket';

function ResourcePortal({children, resource } : any) {
  const [container] = React.useState(() => {
    return document.createElement('div');
  });

  React.useEffect(() => {
    var parent = document.getElementById(resource.resourceId);
    parent?.appendChild(container)
    return () => {
      parent?.removeChild(container)
    }
  }, [])

  return ReactDOM.createPortal(children, container)
}

function Resource({children, resource } : any) {
  const [visible, setVisible] = useState('false');
  const onOpen = useCallback(() => { setVisible('true') }, []);
  const onClose = useCallback(() => { setVisible('false') }, []);
  const onClick = useCallback(() => { document.getElementById(resource.resourceId)?.scrollIntoView() }, []);
  const onClickBack = useCallback(() => { document.getElementById('map')?.scrollIntoView() }, []);
  const onClickClose = useCallback(() => { setVisible('false') }, []);
  return (
    <>
      <Tooltip>{resource.title}</Tooltip>
      <Popup onOpen={ onOpen } onClose={ onClose }>
        <a onClick={ onClick } className="button is-block is-clipped">{resource.title}</a>
      </Popup>
      <ResourcePortal resource={resource}>
        {visible == 'true' &&
          <nav className="level is-overlay" style={ { backgroundColor: 'rgba(10, 10, 10, 0.1)' } }>
            <div className="level-item has-text-centered">
              <div className="buttons">
                <button onClick={ onClickBack } className="button box">Tilbake til kart</button>
                <button onClick={ onClickClose } className="button box">
                  <span className="delete"></span>
                </button>
              </div>
            </div>
          </nav>
        }
      </ResourcePortal>
    </>
  )
}

function MapContent({resource, resources} : any) {
  const [primaryMapLayer, setPrimaryMapLayer] = useState<any[]>([]);
  const [seondaryMapLayer, setSeondaryMapLayer] = useState<any[]>([]);
  const map = useMap();

  useEffect(() => {
    const fetchResourceData = async (resources: string[], primary : boolean) => {
      const resourceData = await Promise.all(resources.map(r => fetch("/Studio/api/Resource/" + r).then(r => r.json())));
      const fillOpacity = (primary) ? 0.2 : 0.1;
      const pathOptions= (primary) ? { color: 'blue' } : { color: 'red' };
      const responsecomponents =
        [].concat(...resourceData).flatMap((resource: any, rindex: Number) => {
          return resource.properties.filter((p: any) => p.tags.includes("@wkt")).flatMap((property: any, pindex: Number) => {
            return property.value.flatMap((value: string, vindex: Number) => {
              const wkt = new Wkt.Wkt().read(value);
              if (wkt.type === "point") {
                return [
                  <Marker key={rindex + "-" + pindex + "-" + vindex} position={[wkt.components[0].y, wkt.components[0].x]} icon={L.divIcon()}>
                    <Resource resource={resource}></Resource>
                  </Marker>
                ]
              } else if (wkt.type === "linestring") {
                return [
                  <Polyline key={rindex + "-" + pindex + "-" + vindex} positions={ wkt.components.map((c : any) => { return [c.y, c.x] } ) }>
                    <Resource resource={resource}></Resource>
                  </Polyline>
                ]
              } else if (wkt.type === "multipoint") {
                var multipoints =
                  wkt.components.map(( point : any, cindex: Number) => {
                    return (
                      <Marker key={rindex + "-" + pindex + "-" + vindex + "-" + cindex} position={ [point[0].y, point[0].x] } icon={L.divIcon()}>
                        <Resource resource={resource}></Resource>
                      </Marker>
                    )
                  });

                return multipoints;
              } else if (wkt.type === "multilinestring") {
                return [
                  <Polyline key={rindex + "-" + pindex + "-" + vindex} positions={wkt.components[0].map((c : any) => { return [c.y, c.x] } )}>
                    <Tooltip>{resource.title}</Tooltip>
                  </Polyline>
                ]
              } else if (wkt.type === "multipolygon") {
                var multipolygon =
                  wkt.components.map(( polygon : any, cindex: Number) => {
                    const polygonpositions = (polygon.length > 1)
                      ? polygon.map((co: any) => { return co.map((c: any) => { return [c.y, c.x] } ) })
                      : polygon[0].map((c: any) => { return [c.y, c.x] } );

                    return (
                      <Polygon key={rindex + "-" + pindex + "-" + vindex + "-" + cindex} positions={ polygonpositions } fillOpacity={fillOpacity} pathOptions={pathOptions}>
                        <Resource resource={resource}></Resource>
                      </Polygon>
                    )
                  });

                return multipolygon;
              } else {
                const polygonpositions = (wkt.components.length > 1)
                  ? wkt.components.map((co: any) => { return co.map((c: any) => { return [c.y, c.x] } ) })
                  : wkt.components[0].map((c: any) => { return [c.y, c.x] } );
                return [
                  <Polygon key={rindex + "-" + pindex + "-" + vindex} positions={ polygonpositions } fillOpacity={fillOpacity} pathOptions={pathOptions}>
                      <Resource resource={resource}></Resource>
                  </Polygon>
                ]
              }
            });
          });
        });

        if (primary) {
          const positions = [].concat(...responsecomponents.map((m: any) => (m.props.position) ? [m.props.position] : m.props.positions));
          if (positions.length > 1 && positions.some((p1: any) => positions.some((p2: any) => p1.join('|') !== p2.join('|')))) {
            map.fitBounds(positions);
          }
          else {
            map.setView(positions[0], map.getZoom());
          }

          setPrimaryMapLayer(responsecomponents);
        } else {
          setSeondaryMapLayer(responsecomponents);
        }
    }

    if (resource) {
      fetchResourceData( resources, false);
      fetchResourceData( [resource], true);
    } else {
      fetchResourceData( resources, true);
    }
  }, [resource, resources, map]);

  return (
      <>
        <TileLayer
          attribution='&copy; <a href="http://osm.org/copyright">OpenStreetMap</a> contributors'
          url="https://a.tile.openstreetmap.org/{z}/{x}/{y}.png "
        />
        <LayersControl position="topright">
          <LayersControl.BaseLayer name="Topologisk Norgeskart" checked={true}>
            <LayerGroup>
              <TileLayer
                url="https://opencache.statkart.no/gatekeeper/gk/gk.open_gmaps?layers=topo4&zoom={z}&x={x}&y={y}"
                attribution="<a href='http://www.kartverket.no'>Kartverket</a>"
              />
            </LayerGroup>
          </LayersControl.BaseLayer>
          <LayersControl.BaseLayer name="Topologisk Norgeskart med Europa">
            <LayerGroup>
              <TileLayer
                url="https://opencache.statkart.no/gatekeeper/gk/gk.open_gmaps?layers=europa&zoom={z}&x={x}&y={y}"
                attribution="<a href='http://www.kartverket.no'>Kartverket</a>"
              />
              <TileLayer
                url="https://opencache.statkart.no/gatekeeper/gk/gk.open_gmaps?layers=topo4&zoom={z}&x={x}&y={y}"
                attribution="<a href='http://www.kartverket.no'>Kartverket</a>"
              />
            </LayerGroup>
          </LayersControl.BaseLayer>
          {seondaryMapLayer.length &&
            <LayersControl.Overlay name="References" checked={false}>
              <LayerGroup>
                {seondaryMapLayer}
              </LayerGroup>
            </LayersControl.Overlay>
          }
        </LayersControl>
        {primaryMapLayer}
      </>
  );
}

function App({resource, resources} : any) {
  const styles : CSSProperties = { position: 'absolute', top: 0, bottom:'0', width: '100%' };

  return (
      <MapContainer zoom={11} scrollWheelZoom={false} touchZoom={false} style={styles}>
        <MapContent resource={resource} resources={resources}/>
        <ScaleControl metric={true} imperial={false}/>
      </MapContainer>
  );
}

export default App;
