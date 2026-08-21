FROM node:22.23.1-alpine3.23 AS dependencies

WORKDIR /source
COPY src/web/package.json src/web/package-lock.json ./
RUN npm ci --no-audit --no-fund

FROM dependencies AS source
COPY src/web/ ./

FROM source AS quality
RUN npm run format:check \
    && npm run lint \
    && npm run typecheck \
    && npm test -- --run

FROM source AS build
RUN npm run build

FROM nginx:1.30.4-alpine3.24 AS runtime

RUN rm /etc/nginx/conf.d/default.conf
COPY deploy/web/nginx.conf /etc/nginx/nginx.conf
COPY --from=build --chown=nginx:nginx /source/dist /usr/share/nginx/html
USER nginx
EXPOSE 8080
ENTRYPOINT ["nginx", "-g", "daemon off;"]
