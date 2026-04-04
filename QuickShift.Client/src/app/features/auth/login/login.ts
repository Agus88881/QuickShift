import { Component, OnInit, NgZone } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router'; // <--- Agregamos ActivatedRoute
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../core/services/auth';

declare var google: any; 

@Component({
  selector: 'app-login',
  templateUrl: './login.html',
  styleUrls: ['./login.scss']
})
export class LoginComponent implements OnInit {
  private tenantName: string = '';

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private ngZone: NgZone
  ) {}

  private static googleInitialized = false;

  ngOnInit(): void {
    this.tenantName = this.route.snapshot.paramMap.get('tenantName') || '';

    const initGoogle = () => {
      if (typeof google !== 'undefined') {
        if (!LoginComponent.googleInitialized) {
          google.accounts.id.initialize({
            client_id: environment.googleClientId,
            callback: this.handleGoogleResponse.bind(this)
          });
          LoginComponent.googleInitialized = true;
        }

        google.accounts.id.renderButton(
          document.getElementById('google-btn'),
          { theme: 'outline', size: 'large', width: 250 }
        );
      } else {
        setTimeout(() => initGoogle(), 500);
      }
    };
    initGoogle();
  }

  handleGoogleResponse(response: any) {
    this.authService.loginWithGoogle(response.credential, this.tenantName).subscribe({
      next: (res: any) => {
        localStorage.setItem('token', res.jwt); 
        localStorage.setItem('tenantName', this.tenantName);
        this.ngZone.run(() => {
          this.router.navigate([`/${this.tenantName}/dashboard`]);
        });
      },
      error: (err) => {
        if (err.status === 401) {
          alert("Acceso denegado: " + err.error.message);
        } else if (err.status === 404) {
          alert("Error: La empresa '" + this.tenantName + "' no existe.");
        } else {
          console.error("Error inesperado:", err);
          alert("Ocurrió un error en el servidor.");
        }
      }
    });
  }
}