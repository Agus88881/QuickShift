import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { JwtHelperService } from '@auth0/angular-jwt'; 
import { environment } from '../../../environments/environment';

export interface ShiftResponse {
  message: string;
  shiftId: number;
  clockIn: string;
  clockOut?: string;
  hoursWorked?: number;
}

export interface ShiftStatus {
  isClockedIn: boolean;
  startTime?: string;
  shiftId?: number;
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
      tenantId = decodedToken["tenantId"] || '0';
    }
    
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'X-Tenant-Id': tenantId 
    });
  }

  getStatus(): Observable<ShiftStatus> {
    return this.http.get<ShiftStatus>(`${this.apiUrl}/status`, { headers: this.getHeaders() });
  }

  clockIn(): Observable<ShiftResponse> {
    return this.http.post<ShiftResponse>(`${this.apiUrl}/clockin`, {}, { headers: this.getHeaders() });
  }

  clockOut(): Observable<ShiftResponse> {
    return this.http.post<ShiftResponse>(`${this.apiUrl}/clockout`, {}, { headers: this.getHeaders() });
  }
}