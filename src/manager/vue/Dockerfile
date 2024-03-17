# Stage 1: Build the Vue.js application
FROM node:lts-alpine as build-stage

WORKDIR /app

COPY package*.json ./

RUN npm install

COPY . .

RUN npm run build

# Stage 2: Serve the application from NGINX
FROM nginx:1.24-alpine3.17 as production-stage

# Remove the default NGINX configuration file
RUN rm /etc/nginx/conf.d/default.conf

# Copy the built files from the build stage
COPY --from=build-stage /app/dist /usr/share/nginx/html

# Create a new NGINX configuration file
RUN echo "server { listen 80; location / { root /usr/share/nginx/html; try_files \$uri \$uri/ /index.html; } }" > /etc/nginx/conf.d/vue-app.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
