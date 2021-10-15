FROM node:latest AS build
WORKDIR /build

COPY package.json package-lock.json ./
RUN npm ci

WORKDIR /build/map
COPY map/package.json ./
RUN npm install
COPY map .
RUN npm run build

FROM nginx:stable-alpine
WORKDIR /usr/share/nginx/html/static
COPY --from=build /build .
COPY studio.css studio.js ./
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]