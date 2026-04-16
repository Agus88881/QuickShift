import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { JwtHelperService } from '@auth0/angular-jwt';
import { environment } from '../../../environments/environment';

declare var google: any;

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private baseUrl = environment.apiUrl;
  private jwtHelper = new JwtHelperService();

  private loggedInSubject = new BehaviorSubject<boolean>(this.hasValidToken());
  public isLoggedIn$ = this.loggedInSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {}

  loginWithGoogle(token: string, tenantName: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/auth/google`, { token, tenantName })
      .pipe(
        tap(response => {
          this.storeToken(response.jwt);
          localStorage.setItem('tenantName', tenantName); 
          
          this.loggedInSubject.next(true);
        })
      );
  }

  logout() {
    const tenantName = localStorage.getItem('tenantName') || 'harveynorman';

    localStorage.removeItem('jwt_token'); 
    
    this.loggedInSubject.next(false);

    if (typeof google !== 'undefined') {
      google.accounts.id.disableAutoSelect();
    }

    this.router.navigate([`/${tenantName}/login`]);
  }

  getToken(): string | null {
    return localStorage.getItem('jwt_token');
  }

  private hasValidToken(): boolean {
    const token = this.getToken();
    if (!token) return false;
    try {
      return !this.jwtHelper.isTokenExpired(token);
    } catch {
      return false;
    }
  }

  private storeToken(token: string): void {
    localStorage.setItem('jwt_token', token);
  }

  inviteUser(email: string, tenantId: number): Observable<any> {
    return this.http.post(`${this.baseUrl}/auth/invite`, { email, tenantId });
  }
}