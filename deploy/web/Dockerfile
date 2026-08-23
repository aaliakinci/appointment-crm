FROM node:22.23.1-alpine3.23@sha256:8516dce0483394d5708d4b2ee6cacb79fb1d617ea4e2787c2120bcca92ce372e AS dependencies

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

FROM nginx:1.30.4-alpine3.24@sha256:97d490c12ba55b4946b01546d1c3ed324e8d41ab1c9fcb2a616aa470620e5b46 AS runtime

ARG VERSION=0.0.0-local
ARG VCS_REF=unknown

LABEL org.opencontainers.image.title="Appointment CRM Web" \
    org.opencontainers.image.version="$VERSION" \
    org.opencontainers.image.revision="$VCS_REF" \
    org.opencontainers.image.source="https://github.com/aaliakinci/appointment_crm"

RUN rm /etc/nginx/conf.d/default.conf
COPY deploy/web/nginx.conf /etc/nginx/nginx.conf
COPY --from=build --chown=nginx:nginx /source/dist /usr/share/nginx/html
USER nginx
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --quiet --spider http://127.0.0.1:8080/ || exit 1
ENTRYPOINT ["nginx", "-g", "daemon off;"]
