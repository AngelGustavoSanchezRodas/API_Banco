# API Banco

API bancaria en .NET 10 con arquitectura en capas (Domain, Application, Infrastructure, WebAPI). Expone operaciones para cuentahabientes, cuentas, tarjetas y pagos de servicios. La persistencia se realiza con Entity Framework Core hacia MySQL.

## Arquitectura (capa por capa)
### Domain
Capa con el modelo de negocio y entidades puras. Contiene:
- Entidades: `Cliente`, `Cuenta`, `TarjetaDebito`, `TransaccionBanco`, `UsuarioAcceso`, `RegistroPagoServicio`, etc.
- Reglas de negocio encapsuladas (ej. `Cuenta.Debitar` y `Cuenta.Acreditar`).
- No depende de infraestructura ni de frameworks externos.

### Application
Capa de casos de uso y contratos. Contiene:
- Servicios de aplicación (orquestan flujos de negocio).
- DTOs de entrada/salida.
- Interfaces de repositorios y servicios externos.
- Validaciones de entrada simples.

### Infrastructure
Capa de acceso a datos y adaptadores. Contiene:
- `BancoDbContext` con mapeos a MySQL.
- Repositorios EF Core (implementan las interfaces de Application).
- Integraciones HTTP con servicios externos.

### WebAPI
Capa de presentación. Contiene:
- Controllers y rutas HTTP.
- Configuración de DI, CORS y middlewares.
- Conexión entre HTTP y servicios de Application.

## Configuración
- `appsettings.json`: plantilla pública (sin secretos).
- `appsettings.Development.json`: credenciales locales y URLs de integración.

Variables relevantes:
- `ConnectionStrings:DefaultConnection`
- `Integraciones:UniversidadApiUrl`, `Integraciones:EnergiaApiUrl`, `Integraciones:TelefoniaApiUrl`

## Servicios de Aplicación
### CuentahabienteServicio
Casos de uso para perfiles, cuentas y tarjetas:
- **Crear perfil**: registra cliente, crea usuario de acceso (rol CLIENTE) y devuelve usuario (DPI) y contraseña temporal.
- **Apertura de cuenta**: crea una cuenta con saldo inicial.
- **Emisión de tarjeta débito**: genera PIN, CVV, vencimiento y número de tarjeta; devuelve los datos al cliente.

### OperacionesFinancierasServicio
- Depósitos, retiros y consulta de saldo.
- Registra movimientos en bitácora (`TransaccionBanco`).

### PagoServiciosServicio
- Valida identificadores y ejecuta pagos de servicios con distribución 95/5.
- Registra débito del cuentahabiente, acreditación a la empresa y comisión del banco.
- Persiste auditoría en `RegistroPagoServicio`.

### BitacoraServicio
- Consulta de movimientos por cuenta en orden cronológico.

## Controllers
### CuentahabientesController
- `POST /api/cuentahabientes/perfil`: crear perfil de cuentahabiente.
- `POST /api/cuentahabientes/tarjeta`: emitir tarjeta débito.

### OperacionesController
- Depósitos, retiros y consulta de saldo.

### PagosController
- `POST /api/pagos/validar`: valida identificador de servicio.
- `POST /api/pagos/ejecutar`: ejecuta pago de servicio.
- `GET /api/pagos/consultar-deuda/{tipo}/{identificador}`: consulta deuda externa.

### AuthController
- `POST /api/auth/login`: login con correo o DPI (NombreUsuario). Responde 401 si falla.

## Persistencia (DbContext)
- `BancoDbContext` mapea entidades a tablas en snake_case.
- Relaciones clave:
  - Cliente 1..N Cuentas
  - Cuenta 1..1 TarjetaDebito
  - UsuarioAcceso N..1 Cliente
  - TransaccionBanco N..1 Cuenta

## Integraciones
- `GestorIntegracionServicios` centraliza llamadas a APIs externas (Universidad/Energía/Telefonía).
- BaseAddress de HTTP client configurado por `Integraciones:UniversidadApiUrl`.

## Seguridad
- Los secretos no se incluyen en `appsettings.json`.
- `appsettings.Development.json` no debe versionarse.

## Compilación
```bash
dotnet build
```

## Notas
- Contraseñas se manejan de forma simple por MVP (sin hashing complejo).
- CORS está configurado para permitir cualquier origen (temporal para integración con front).