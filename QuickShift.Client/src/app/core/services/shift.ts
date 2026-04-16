import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { JwtHelperService } from '@auth0/angular-jwt'; // <--- Importamos esto
import { environment } from '../../../environments/environment';

export interface ShiftResponse {
  message: string;
  shiftId: number;
  clockIn: string;
  clockOut?: string;
  hoursWorked?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ShiftService {
  private apiUrl = `${environment.apiUrl}/shift`;
  private jwtHelper = new JwtHelperService();

  constructor(private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('jwt_token');
    let tenantId = '0';

    if (token) {
      const decodedToken = this.jwtHelper.decodeToken(token);
      // Extraemos el tenantId que guardamos en el Backend (C#)
      tenantId = decodedToken["tenantId"] || '0';
    }
    
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'X-Tenant-Id': tenantId 
    });
  }

  clockIn(): Observable<ShiftResponse> {
    return this.http.post<ShiftResponse>(`${this.apiUrl}/clockin`, {}, { headers: this.getHeaders() });
  }

  clockOut(): Observable<ShiftResponse> {
    return this.http.post<ShiftResponse>(`${this.apiUrl}/clockout`, {}, { headers: this.getHeaders() });
  }
}