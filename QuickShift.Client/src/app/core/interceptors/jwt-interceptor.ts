import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth';
import { environment } from '../../../environments/environment';

@Injectable()
export class JwtInterceptor implements HttpInterceptor {

  constructor(private authService: AuthService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    // 1. Obtenemos el token desde nuestro AuthService
    const token = this.authService.getToken();
    
    // 2. Verificamos si la petición va hacia nuestra API de .NET
    const isApiUrl = request.url.startsWith(environment.apiUrl);

    // 3. Si tenemos token y la petición va a nuestra API, clonamos la petición y le inyectamos el JWT
    if (token && isApiUrl) {
      request = request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }

    // 4. Dejamos que la petición siga su curso (ahora armada con el token)
    return next.handle(request);
  }
}