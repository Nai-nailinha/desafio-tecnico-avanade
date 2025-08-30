# 🚀 Desafio Técnico Avanade – Microserviços em .NET

Plataforma de e-commerce com **Arquitetura de Microserviços**, implementada em .NET:

- 📦 **InventoryService** (Estoque)  
- 🛒 **SalesService** (Vendas)  
- 🌐 **ApiGateway** (YARP + JWT)  
- 📨 **RabbitMQ** (mensageria entre serviços)  
- 💾 Banco: **InMemory** (modo dev simplificado)


## 🛠 Tecnologias
- [.NET 8/9](https://dotnet.microsoft.com/) + C#  
- Minimal APIs + Entity Framework Core (InMemory)  
- [YARP Reverse Proxy](https://microsoft.github.io/reverse-proxy/) (API Gateway)  
- JWT (didático)  
- RabbitMQ (opcional em dev)


## ⚙️ Como rodar (modo simples – sem Docker)

### 1) InventoryService (porta **5081**)
```powershell
dotnet run --project .\InventoryService\InventoryService.csproj --urls http://localhost:5081
Health: http://localhost:5081/health
```
 → inventory ok
 
 → Swagger: http://localhost:5081/swagger

### 2) SalesService (porta 5082)
```
dotnet run --project .\SalesService\SalesService.csproj --urls http://localhost:5082
```
Health: http://localhost:5082/health
 → sales ok

 → Swagger: http://localhost:5082/swagger

### 3) (Opcional) API Gateway (porta 5080)
```
dotnet run --project .\ApiGateway\ApiGateway.csproj --urls http://localhost:5080
```

Health: http://localhost:5080/
 → API Gateway ok
Login (JWT): POST http://localhost:5080/login
```
{
  "username": "admin",
  "password": "admin"
}
```
🧪 Testes rápidos (sem Gateway)

Criar produto (Inventory):
```
Invoke-RestMethod -Method Post http://localhost:5081/products `
  -ContentType "application/json" `
  -Body '{"name":"Mouse Gamer","description":"RGB","price":199.90,"quantity":10}'
```

Criar pedido (Sales → valida estoque no Inventory):
```
Invoke-RestMethod -Method Post http://localhost:5082/orders `
  -ContentType "application/json" `
  -Body '{"items":[{"productId":1,"quantity":2}]}'
```

Conferir estoque (Inventory):
```
Invoke-RestMethod http://localhost:5081/products/1
```

🔑 Testes via Gateway (com JWT)
```
# login
$resp  = Invoke-RestMethod -Method Post http://localhost:5080/login `
  -ContentType "application/json" `
  -Body '{"username":"admin","password":"admin"}'

$token = $resp.token

# listar produtos (Inventory via Gateway)
Invoke-RestMethod http://localhost:5080/inventory/products `
  -Headers @{ Authorization = "Bearer $token" }

# criar pedido (Sales via Gateway)
Invoke-RestMethod -Method Post http://localhost:5080/sales/orders `
  -Headers @{ Authorization = "Bearer $token" } `
  -ContentType "application/json" `
  -Body '{"items":[{"productId":1,"quantity":1}]}'
```

📬 RabbitMQ (opcional)

O Inventory consome mensagens de order_confirmed e o Sales publica esse evento.

Se quiser rodar RabbitMQ localmente:
```
# docker-compose.yml
version: "3.9"
services:
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672" # console: guest/guest
```

📂 Estrutura
```
desafio-tecnico-avanade/
  ApiGateway/
  InventoryService/
  SalesService/
  Shared/
  README.md
  .gitignore
  docker-compose.yml (opcional)
```

📝 Notas

* Para produção → trocar InMemory por SQL Server + Migrations.
* JWT no Gateway é apenas didático (usuário fixo: admin/admin).
* Melhorias possíveis: logs (Serilog), correlação de requests, testes xUnit.


## 👩‍💻 Autor

Feito com 💜 por **Enaile Vasconcelos**

[![WhatsApp](https://img.shields.io/badge/WhatsApp-25D366?style=for-the-badge&logo=whatsapp&logoColor=white)](https://wa.me/5581996062303)
[![Email](https://img.shields.io/badge/Email-D14836?style=for-the-badge&logo=gmail&logoColor=white)](mailto:enaile@ticomcafeeneuronios.com.br)
[![Site](https://img.shields.io/badge/Site-000000?style=for-the-badge&logo=About.me&logoColor=white)](https://ticomcafeeneuronios.com.br)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-0077B5?style=for-the-badge&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/enailelopes)
