# FoodDelivery-Microservice-DotnetCore
Food delivery microservice project used by  .Net Core 5.0 , Code First , Entity Framework , Ocelot , RabbitMQ , CQRS , Mediatr , JWT , Elastic Search


Features

• Customer user can search restaurants near the address by Address Service.

• Customer user can add order food to basket by Basket Service , if paid , can see order status by Order Service.

• Restaurant user can save and edit their menu by Product Service.

• Restaurant user can change order status by Order Service.

• All of transactions logs save in Notification Service

• When users login the system , User and Restaurant Service generate JWT for authentication and authorization.

• Services communicate each others by RabbitMQ.
